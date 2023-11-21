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
using System.Net.Http;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Application.GridAreas;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Configuration;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.Infrastructure.Actors;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.FeatureFlag;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.Infrastructure.Configuration.MessageBus;
using Energinet.DataHub.EDI.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using Energinet.DataHub.EDI.Infrastructure.GridAreas;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using MediatR;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration
{
    public class CompositionRoot
    {
        private readonly IServiceCollection _services;

        private CompositionRoot(IServiceCollection services)
        {
            _services = services;
            services.AddSingleton<HttpClient>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddScoped<ITransactionIdRepository, TransactionIdRepository>();
            services.AddScoped<IMessageIdRepository, MessageIdRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFeatureFlagProvider, FeatureFlagProviderProvider>();

            AddMediatR();
            services.AddLogging();
            AddActorServices();
            AddWholeSaleInBox();
            AddGridAreaServices();
            IntegrationEventsConfiguration.Configure(services);
            InboxEventsConfiguration.Configure(services);
            ArchivedMessageConfiguration.Configure(services);
            QueryHandlingConfiguration.Configure(services);
            DataRetentionConfiguration.Configure(services);
        }

        public static CompositionRoot Initialize(IServiceCollection services)
        {
            return new CompositionRoot(services);
        }

        public CompositionRoot AddMessageBus(string connectionString)
        {
            _services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(connectionString));
            _services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
            return this;
        }

        public CompositionRoot AddDatabaseContext(string databaseConnectionString)
        {
            _services.AddScoped<SqlConnectionSource>(sp => new SqlConnectionSource(databaseConnectionString!));
            _services.AddScopedSqlDbContext<B2BContext>();
            return this;
        }

        public CompositionRoot AddSystemClock(ISystemDateTimeProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _services.AddScoped(sp => provider);
            return this;
        }

        public CompositionRoot AddBearerAuthentication(TokenValidationParameters tokenValidationParameters)
        {
            _services.AddScoped<CurrentClaimsPrincipal>();
            _services.AddScoped(sp => new JwtTokenParser(tokenValidationParameters));
            return this;
        }

        public CompositionRoot AddAuthentication(Func<IServiceProvider, IMarketActorAuthenticator>? authenticatorBuilder = null)
        {
            if (authenticatorBuilder is null)
            {
                _services.AddScoped<IMarketActorAuthenticator, MarketActorAuthenticator>();
            }
            else
            {
                _services.AddScoped(authenticatorBuilder);
            }

            return this;
        }

        public CompositionRoot AddDatabaseConnectionFactory(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            _services.AddSingleton<IDatabaseConnectionFactory>(_ => new SqlDatabaseConnectionFactory(connectionString));
            return this;
        }

        public CompositionRoot AddCorrelationContext(Func<IServiceProvider, ICorrelationContext> action)
        {
            _services.AddScoped(action);
            return this;
        }

        public CompositionRoot AddRequestLogging(string blobStorageConnectionString, string storageContainerName)
        {
            if (blobStorageConnectionString == null) throw new ArgumentNullException(nameof(blobStorageConnectionString));
            if (storageContainerName == null) throw new ArgumentNullException(nameof(storageContainerName));
            _services.AddSingleton<IRequestResponseLogging>(s =>
            {
                var factory = s.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger<RequestResponseLoggingBlobStorage>();
                return new RequestResponseLoggingBlobStorage(blobStorageConnectionString, storageContainerName, logger);
            });
            return this;
        }

        public CompositionRoot AddMessagePublishing()
        {
            _services.AddScoped<IActorRepository, ActorRepository>();
            return this;
        }

        public CompositionRoot AddMessageParserServices()
        {
            IncomingMessageParsingServices.AddIncomingMessageParsingServices(_services);
            return this;
        }

        public CompositionRoot AddRemoteBusinessService<TRequest, TReply>(string remoteRequestQueueName, string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton<IRemoteBusinessServiceRequestSenderAdapter<TRequest>>(provider =>
                new RemoteBusinessServiceRequestSenderAdapter<TRequest>(provider.GetRequiredService<ServiceBusClient>(), remoteRequestQueueName));
            AddRemoteBusinessService<TRequest, TReply>(responseQueueName);
            return this;
        }

        public CompositionRoot AddRemoteBusinessService<TRequest, TReply>(Func<IServiceProvider, IRemoteBusinessServiceRequestSenderAdapter<TRequest>> adapterBuilder, string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton(adapterBuilder);
            AddRemoteBusinessService<TRequest, TReply>(responseQueueName);
            return this;
        }

        private void AddRemoteBusinessService<TRequest, TReply>(string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton(provider =>
                new RemoteBusinessService<TRequest, TReply>(
                    provider.GetRequiredService<IRemoteBusinessServiceRequestSenderAdapter<TRequest>>(),
                    responseQueueName));
        }

        private void AddMediatR()
        {
            var configuration = new MediatRServiceConfiguration();
            ServiceRegistrar.AddRequiredServices(_services, configuration);
        }

        private void AddActorServices()
        {
            _services.AddScoped<IRequestHandler<CreateActorCommand, Unit>, CreateActorHandler>();
            _services.AddScoped<IActorRegistry, ActorRegistry>();
        }

        private void AddGridAreaServices()
        {
            _services.AddScoped<IRequestHandler<GridAreaOwnershipAssignedCommand, Unit>, GridAreaOwnershipAssignedHandler>();
            _services.AddScoped<IGridAreaRepository, GridAreaRepository>();
        }

        private void AddWholeSaleInBox()
        {
            WholesaleInboxConfiguration.Configure(_services);
        }
    }
}
