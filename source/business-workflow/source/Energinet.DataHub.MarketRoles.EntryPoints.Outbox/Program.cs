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
using Energinet.DataHub.MarketRoles.Application.Integration;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
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
    public static class Program
    {
        public static async Task Main()
        {
            var container = new Container();
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
                        var connectionString = Environment.GetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING")
                                               ?? throw new InvalidOperationException(
                                                   "Metering point db connection string not found.");

                        x.UseSqlServer(connectionString, y => y.UseNodaTime());
                    });

                    services.AddSimpleInjector(container, options =>
                    {
                        options.AddLogging();
                    });
                })
                .Build()
                .UseSimpleInjector(container);

            // Register application components.
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            container.Register<IOutboxManager, OutboxManager>(Lifestyle.Scoped);
            container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<EventMessageDispatcher>(Lifestyle.Transient);
            container.Register<IIntegrationEventDispatchOrchestrator, IntegrationEventDispatchOrchestrator>(Lifestyle.Transient);

            container.BuildMediator(
                new[]
                {
                    typeof(ConsumerMoveInAccepted).Assembly,
                },
                Array.Empty<Type>());

            container.Verify();

            await host.RunAsync().ConfigureAwait(false);

            await container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
