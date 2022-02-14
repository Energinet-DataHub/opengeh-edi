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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Application.Common.Users;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Application.MoveIn.Validation;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.SimpleInjector;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using Energinet.DataHub.MarketRoles.Infrastructure;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline;
using Energinet.DataHub.MarketRoles.Infrastructure.ContainerExtensions;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.ProcessManagers;
using Energinet.DataHub.MarketRoles.Infrastructure.DomainEventDispatching;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.Notifications;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Energinet.DataHub.MarketRoles.Infrastructure.Messaging.Idempotency;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Users;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

[assembly: CLSCompliant(false)]

namespace Energinet.DataHub.MarketRoles.EntryPoints.Processing
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
            options.UseMiddleware<EntryPointTelemetryScopeMiddleware>();
            options.UseMiddleware<ServiceBusUserContextMiddleware>();
        }

        protected override void ConfigureServiceCollection(IServiceCollection services)
        {
            base.ConfigureServiceCollection(services);

            services.AddDbContext<MarketRolesContext>(x =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                       ?? throw new InvalidOperationException(
                                           "database connection string not found.");

                x.UseSqlServer(connectionString, y => y.UseNodaTime());
            });
        }

        protected override void ConfigureContainer(Container container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            container.Register<QueueSubscriber>(Lifestyle.Scoped);
            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<ServiceBusUserContextMiddleware>(Lifestyle.Scoped);
            container.Register<IUserContext, UserContext>(Lifestyle.Scoped);
            container.Register<UserIdentityFactory>(Lifestyle.Singleton);
            container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            container.Register<ISystemDateTimeProvider, SystemDateTimeProvider>(Lifestyle.Scoped);
            container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            container.Register<IProcessManagerRepository, ProcessManagerRepository>(Lifestyle.Scoped);
            container.Register<IConsumerRepository, ConsumerRepository>(Lifestyle.Scoped);
            container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Scoped);
            container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Scoped);
            container.Register<ICommandScheduler, CommandScheduler>(Lifestyle.Scoped);
            container.Register<IDomainEventsAccessor, DomainEventsAccessor>(Lifestyle.Scoped);
            container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>(Lifestyle.Scoped);
            container.Register<IDomainEventPublisher, DomainEventPublisher>(Lifestyle.Scoped);
            container.Register<ServiceBusMessageIdempotencyMiddleware>(Lifestyle.Scoped);
            container.Register<IIncomingMessageRegistry, IncomingMessageRegistry>(Lifestyle.Transient);
            container.Register<IProtobufMessageFactory, ProtobufMessageFactory>(Lifestyle.Singleton);
            container.Register<INotificationReceiver, NotificationReceiver>(Lifestyle.Scoped);
            container.Register<IntegrationEventReceiver>(Lifestyle.Scoped);

            var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING")
                                   ?? throw new InvalidOperationException(
                                       "database connection string not found.");
            container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(connectionString), Lifestyle.Scoped);

            container.BuildMediator(
                new[]
                {
                    typeof(RequestChangeOfSupplierHandler).Assembly,
                    typeof(PublishWhenEnergySupplierHasChanged).Assembly,
                },
                new[]
                {
                    typeof(UnitOfWorkBehaviour<,>),
                    typeof(AuthorizationBehaviour<,>),
                    typeof(InputValidationBehaviour<,>),
                    typeof(DomainEventsDispatcherBehaviour<,>),
                    typeof(InternalCommandHandlingBehaviour<,>),
                    typeof(BusinessProcessResponderBehaviour<,>),
                });

            container.ReceiveProtobuf<Contracts.MarketRolesEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.MarketRolesMessagesCase)
                    .WithParser(() => Contracts.MarketRolesEnvelope.Parser));

            container.SendProtobuf<Energinet.DataHub.MarketRoles.IntegrationEventContracts.EnergySupplierChanged>();

            // Actor Notification handlers
            container.Register<IEndOfSupplyNotifier, EndOfSupplyNotifier>(Lifestyle.Scoped);
            container.Register<IConsumerDetailsForwarder, ConsumerDetailsForwarder>(Lifestyle.Scoped);
            container.Register<IMeteringPointDetailsForwarder, MeteringPointDetailsForwarder>(Lifestyle.Scoped);

            // Business process responders
            container.Register<IBusinessProcessResultHandler<RequestChangeOfSupplier>, RequestChangeOfSupplierResultHandler>(Lifestyle.Scoped);
            container.Register<IBusinessProcessResultHandler<RequestMoveIn>, RequestMoveInResultHandler>(Lifestyle.Scoped);

            // Input validation(
            container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);
            container.Register<IValidator<RequestMoveIn>, RequestMoveInRuleSet>(Lifestyle.Scoped);
            container.AddValidationErrorConversion(
                validateRegistrations: false,
                typeof(RequestMoveIn).Assembly, // Application
                typeof(ConsumerMovedIn).Assembly, // Domain
                typeof(ErrorMessageFactory).Assembly); // Infrastructure
        }
    }
}
