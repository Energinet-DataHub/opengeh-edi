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
using Energinet.DataHub.Core.App.FunctionApp.Middleware;
using Energinet.DataHub.Core.App.FunctionApp.SimpleInjector;
using Energinet.DataHub.MarketRoles.Application.Common.Users;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.XmlConverter.Mappings;
using Energinet.DataHub.MarketRoles.Infrastructure.Ingestion;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
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
            if (options == null) throw new ArgumentNullException(nameof(options));
            base.ConfigureFunctionsWorkerDefaults(options);

            options.UseMiddleware<JwtTokenMiddleware>();
            options.UseMiddleware<ActorMiddleware>();
            options.UseMiddleware<CorrelationIdMiddleware>();
            options.UseMiddleware<EntryPointTelemetryScopeMiddleware>();
        }

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            base.ConfigureServiceCollection(services);
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            container.Register<CommandApi>(Lifestyle.Scoped);
            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<IUserContext, UserContext>(Lifestyle.Scoped);

            var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException(
                "B2C tenant id not found.");
            var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException(
                "Backend service app id not found.");

            container.AddJwtTokenSecurity($"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration", audience);
            container.AddActorContext<ActorProvider>();
            var dbConnectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                     ?? throw new InvalidOperationException(
                                         "database connection string not found.");
            container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(dbConnectionString), Lifestyle.Scoped);
            Dapper.SqlMapper.AddTypeHandler(NodaTimeSqlMapper.Instance);

            container.Register(XmlMapperFactory, Lifestyle.Singleton);
            container.Register<XmlMapper>(Lifestyle.Singleton);
            container.Register<IXmlDeserializer, XmlDeserializer>(Lifestyle.Singleton);

            container.SendProtobuf<MarketRolesEnvelope>();

            container.Register<MessageDispatcher>(Lifestyle.Scoped);
            container.Register<Channel, ProcessingServiceBusChannel>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_CONNECTION_STRING");
            var topicName = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_TOPIC_NAME");
            container.Register<ServiceBusSender>(
                () => new ServiceBusClient(connectionString).CreateSender(topicName),
                Lifestyle.Singleton);
            container.Verify();
        }

        private Func<string, string, XmlMappingConfigurationBase> XmlMapperFactory()
        {
            return (processType, type) =>
            {
                switch (processType)
                {
                    case "E03":
                        return new ChangeOfSupplierXmlMappingConfiguration();
                    case "E65":
                        return new RequestMoveInXmlMappingConfiguration();
                    default:
                        throw new NotImplementedException($"Found no mapper for process type {processType} and type {type}");
                }
            };
        }
    }
}
