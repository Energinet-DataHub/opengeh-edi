using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;

namespace Energinet.DataHub.MarketRoles.EntryPoints.InternalCommandDispatcher
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

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            base.ConfigureServiceCollection(services);

            services.AddDbContext<MarketRolesContext>(x =>
            {
                var dbConnectionString = Environment.GetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING")
                                         ?? throw new InvalidOperationException(
                                             "Market roles point db connection string not found.");

                x.UseSqlServer(dbConnectionString, options => options.UseNodaTime());
            });
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            // Register application components.
            container.Register<Dispatcher>(Lifestyle.Scoped);
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<IInternalCommandAccessor, InternalCommandAccessor>(Lifestyle.Scoped);
            container.Register<IInternalCommandProcessor, InternalCommandProcessor>(Lifestyle.Scoped);
            container.Register<IInternalCommandDispatcher, InternalCommandServiceBusDispatcher>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("MARKETROLES_QUEUE_CONNECTION_STRING");
            var topicName = Environment.GetEnvironmentVariable("MARKETROLES_QUEUE_TOPIC_NAME");
            container.Register<ServiceBusSender>(
                () => new ServiceBusClient(connectionString).CreateSender(topicName),
                Lifestyle.Singleton);

            container.AddProtobufMessageSerializer();
            container.AddProtobufOutboundMappers(
                new[]
                {
                    typeof(EnergySupplierChangedIntegrationEvent).Assembly,
                });
        }
    }
}
