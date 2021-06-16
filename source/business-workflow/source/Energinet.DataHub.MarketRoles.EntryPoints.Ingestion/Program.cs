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
using Energinet.DataHub.MarketRoles.Application.Common.Users;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.EntryPoints.Ingestion.Middleware;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
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
                    options.UseMiddleware<HttpCorrelationIdMiddleware>();
                    options.UseMiddleware<HttpUserContextMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    var descriptor = new ServiceDescriptor(
                        typeof(IFunctionActivator),
                        typeof(SimpleInjectorActivator),
                        ServiceLifetime.Singleton);
                    services.Replace(descriptor); // Replace existing activator

                    services.AddLogging();
                    services.AddSimpleInjector(container, options =>
                    {
                        options.AddLogging();
                    });

                    services.SendProtobuf<MarketRolesEnvelope>();
                })
                .Build()
                .UseSimpleInjector(container);

            // Register application components.
            container.Register<CommandApi>(Lifestyle.Scoped);
            container.Register<HttpCorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<HttpUserContextMiddleware>(Lifestyle.Scoped);
            container.Register<IUserContext, UserContext>(Lifestyle.Scoped);

            container.Register<MessageDispatcher>(Lifestyle.Scoped);

            // TODO: add service bus implementation
            container.Register<Channel, NullChannel>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_CONNECTION_STRING");
            var topicName = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_TOPIC_NAME");
            container.Register<ServiceBusSender>(
                () => new ServiceBusClient(connectionString).CreateSender(topicName),
                Lifestyle.Singleton);
            container.Verify();

            await host.RunAsync().ConfigureAwait(false);

            await container.DisposeAsync().ConfigureAwait(false);
        }
    }
}
