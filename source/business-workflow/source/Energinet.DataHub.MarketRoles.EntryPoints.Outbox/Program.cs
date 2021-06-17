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
using System.Data;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Application.Integration;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.Services;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

[assembly: CLSCompliant(false)]

namespace Energinet.DataHub.MarketRoles.EntryPoints.Outbox
{
    public class Program
    {
        private readonly Container _container;

        public Program()
            : this(new Container())
        { }

        public Program(Container container)
        {
            _container = container;
        }

        public static async Task Main()
        {
            var prg = new Program();

            var host = prg.ConfigureApplication();
            prg.AssertConfiguration();
            await prg.ExecuteApplicationAsync(host).ConfigureAwait(false);
        }

        public IHost ConfigureApplication()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(options =>
                {
                    options.UseMiddleware<SimpleInjectorScopedRequest>();
                })
                .ConfigureServices(services =>
                {
                    var descriptor = new ServiceDescriptor(
                        typeof(IFunctionActivator),
                        typeof(SimpleInjectorActivator),
                        ServiceLifetime.Singleton);
                    services.Replace(descriptor); // Replace existing activator

                    services.AddLogging();

                    services.AddDbContext<MarketRolesContext>(x =>
                    {
                        var dbConnectionString = Environment.GetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING")
                                               ?? throw new InvalidOperationException(
                                                   "Metering point db connection string not found.");

                        x.UseSqlServer(dbConnectionString, y => y.UseNodaTime());
                    });

                    services.AddSimpleInjector(_container, options =>
                    {
                        options.AddLogging();
                    });
                })
                .Build()
                .UseSimpleInjector(_container);

            // Register application components.
            _container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            _container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            _container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            _container.Register<IOutboxManager, OutboxManager>(Lifestyle.Scoped);
            _container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            _container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            _container.Register<EventMessageDispatcher>(Lifestyle.Transient);
            _container.Register<IIntegrationEventDispatchOrchestrator, IntegrationEventDispatchOrchestrator>(Lifestyle.Transient);

            var connectionString = Environment.GetEnvironmentVariable("SHARED_SERVICEBUS_INTEGRATION_EVENT_CONNECTIONSTRING_TODO");
            _container.Register<ServiceBusClient>(
                () => new ServiceBusClient(connectionString),
                Lifestyle.Singleton);

            _container.Register<ConsumerRegisteredTopic>(() => new ConsumerRegisteredTopic("TOPICNAME"), Lifestyle.Singleton);
            _container.Register<TopicSender<ConsumerRegisteredTopic>>(Lifestyle.Singleton);
            // RegisterTopic<ConsumerRegisteredTopic>(container, Environment.GetEnvironmentVariable("CONSUMER_REGISTERED_TOPIC_TODO") ?? throw new DataException("Couldn't find CONSUMER_REGISTERED_TOPIC_TODO"));
            _container.BuildMediator(
                new[]
                {
                    typeof(ConsumerRegistered).Assembly,
                },
                Array.Empty<Type>());

            return host;
        }

        public void AssertConfiguration() => _container.Verify();

        public async Task ExecuteApplicationAsync(IHost host)
        {
            await host.RunAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
