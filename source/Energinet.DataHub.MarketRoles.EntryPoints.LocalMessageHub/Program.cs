// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using Energinet.DataHub.MarketRoles.EntryPoints.LocalMessageHub.Functions;
using Energinet.DataHub.MarketRoles.Messaging;
using Energinet.DataHub.MarketRoles.Messaging.Bundling;
using Energinet.DataHub.MarketRoles.Messaging.Bundling.Confirm;
using Energinet.DataHub.MarketRoles.Messaging.Bundling.Generic;
using Energinet.DataHub.MarketRoles.Messaging.Bundling.Reject;
using Energinet.DataHub.MessageHub.Client;
using Energinet.DataHub.MessageHub.Client.SimpleInjector;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processing.Domain.SeedWork;
using Processing.Infrastructure;
using Processing.Infrastructure.Correlation;
using Processing.Infrastructure.DataAccess;
using Processing.Infrastructure.DataAccess.MessageHub;
using Processing.Infrastructure.DataAccess.MessageHub.Bundling;
using Processing.Infrastructure.EDI.Acknowledgements;
using Processing.Infrastructure.EDI.GenericNotification;
using Processing.Infrastructure.Ingestion;
using Processing.Infrastructure.LocalMessageHub;
using Processing.Infrastructure.Outbox;
using Processing.Infrastructure.Serialization;
using Processing.Infrastructure.Transport;
using Processing.Infrastructure.Transport.Protobuf.Integration;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.LocalMessageHub
{
    public class Program : EntryPoint
    {
        public static async Task Main()
        {
            var program = new Program();

            var host = program.ConfigureApplication();
            program.AssertConfiguration();
            await program.ExecuteApplicationAsync(host).ConfigureAwait(false);
        }

        protected override void ConfigureFunctionsWorkerDefaults(IFunctionsWorkerApplicationBuilder options)
        {
            base.ConfigureFunctionsWorkerDefaults(options);

            options.UseMiddleware<CorrelationIdMiddleware>();
            options.UseMiddleware<ServiceBusSessionIdMiddleware>();
            options.UseMiddleware<EntryPointTelemetryScopeMiddleware>();
        }

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            base.ConfigureServiceCollection(services);

            services.AddDbContext<MarketRolesContext>(x =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING")
                                       ?? throw new InvalidOperationException(
                                           "Market roles db connection string not found.");

                x.UseSqlServer(connectionString, y => y.UseNodaTime());
            });
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            // Register application components.
            container.Register<RequestBundleQueueSubscriber>(Lifestyle.Scoped);
            container.Register<BundleDequeuedQueueSubscriber>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ISessionContext, SessionContext>(Lifestyle.Scoped);
            container.Register<ServiceBusSessionIdMiddleware>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<IUnitOfWork, UnitOfWork>();
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Singleton);
            container.SendProtobuf<MarketRolesEnvelope>();
            container.Register<IMessageDispatcher, InternalDispatcher>(Lifestyle.Scoped);
            container.Register<Channel, ProcessingServiceBusChannel>(Lifestyle.Scoped); // TODO: internal service bus from MP?
            container.Register<IActorContext, ActorContext>(Lifestyle.Scoped);

            container.UseMediatR()
                .WithPipeline()
                .WithRequestHandlers(
                    typeof(ConfirmMessageBundleHandler),
                    typeof(RejectMessageBundleHandler),
                    typeof(GenericNotificationBundleHandler));

            var messageHubStorageConnectionString = Environment.GetEnvironmentVariable("MESSAGEHUB_STORAGE_CONNECTION_STRING") ?? throw new InvalidOperationException("MessageHub storage connection string not found.");
            var messageHubStorageContainerName = Environment.GetEnvironmentVariable("MESSAGEHUB_STORAGE_CONTAINER_NAME") ?? throw new InvalidOperationException("MessageHub storage container name not found.");
            var messageHubServiceBusConnectionString = Environment.GetEnvironmentVariable("MESSAGEHUB_QUEUE_CONNECTION_STRING") ?? throw new InvalidOperationException("MessageHub queue connection string not found.");

            container.AddMessageHubCommunication(
                messageHubServiceBusConnectionString,
                new MessageHubConfig(
                    Environment.GetEnvironmentVariable("MESSAGEHUB_DATA_AVAILABLE_QUEUE") ?? throw new InvalidOperationException("MessageHub data available queue not found."),
                    Environment.GetEnvironmentVariable("MESSAGEHUB_DOMAIN_REPLY_QUEUE") ?? throw new InvalidOperationException("MessageHub domain reply queue not found.")),
                messageHubStorageConnectionString,
                new StorageConfig(messageHubStorageContainerName));

            container.Register<ILocalMessageHubClient, LocalMessageHubClient>(Lifestyle.Scoped);
            container.Register<IMessageHubMessageRepository, MessageHubMessageRepository>(Lifestyle.Scoped);
            container.Register<IOutboxDispatcher<DataBundleResponse>, DataBundleResponseOutboxDispatcher>();
            container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            container.Register<IBundleCreator, BundleCreator>(Lifestyle.Scoped);
            container.Register<IDocumentSerializer<ConfirmMessage>, ConfirmMessageXmlSerializer>(Lifestyle.Singleton);
            container.Register<IDocumentSerializer<RejectMessage>, RejectMessageXmlSerializer>(Lifestyle.Singleton);
            container.Register<IDocumentSerializer<GenericNotificationMessage>, GenericNotificationMessageXmlSerializer>(Lifestyle.Singleton);

            var connectionString = Environment.GetEnvironmentVariable("METERINGPOINT_QUEUE_CONNECTION_STRING");
            var topic = Environment.GetEnvironmentVariable("METERINGPOINT_QUEUE_TOPIC_NAME");

            container.Register(() => new ServiceBusClient(connectionString).CreateSender(topic), Lifestyle.Singleton);
        }
    }
}
