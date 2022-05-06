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
using Energinet.DataHub.Core.App.Common;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.Core.App.FunctionApp.Middleware;
using Energinet.DataHub.MarketRoles.EntryPoints.Common;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.Telemetry;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Processing.Application.ChangeOfSupplier;
using Processing.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Processing.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Processing.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Processing.Application.ChangeOfSupplier.Validation;
using Processing.Application.Common.Commands;
using Processing.Application.Common.DomainEvents;
using Processing.Application.Common.Processing;
using Processing.Application.EDI;
using Processing.Application.MoveIn;
using Processing.Application.MoveIn.Validation;
using Processing.Domain.Consumers;
using Processing.Domain.EnergySuppliers;
using Processing.Domain.MeteringPoints;
using Processing.Domain.MeteringPoints.Events;
using Processing.Domain.SeedWork;
using Processing.Infrastructure;
using Processing.Infrastructure.BusinessRequestProcessing;
using Processing.Infrastructure.BusinessRequestProcessing.Pipeline;
using Processing.Infrastructure.ContainerExtensions;
using Processing.Infrastructure.Correlation;
using Processing.Infrastructure.DataAccess;
using Processing.Infrastructure.DataAccess.AccountingPoints;
using Processing.Infrastructure.DataAccess.Consumers;
using Processing.Infrastructure.DataAccess.EnergySuppliers;
using Processing.Infrastructure.DataAccess.ProcessManagers;
using Processing.Infrastructure.DomainEventDispatching;
using Processing.Infrastructure.EDI;
using Processing.Infrastructure.EDI.ChangeOfSupplier;
using Processing.Infrastructure.EDI.ChangeOfSupplier.ConsumerDetails;
using Processing.Infrastructure.EDI.ChangeOfSupplier.EndOfSupplyNotification;
using Processing.Infrastructure.EDI.ChangeOfSupplier.MeteringPointDetails;
using Processing.Infrastructure.EDI.MoveIn;
using Processing.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Processing.Infrastructure.Integration.Notifications;
using Processing.Infrastructure.InternalCommands;
using Processing.Infrastructure.Outbox;
using Processing.Infrastructure.Serialization;
using Processing.Infrastructure.Transport.Protobuf;
using Processing.Infrastructure.Transport.Protobuf.Integration;
using Processing.Infrastructure.Users;
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
            options.UseMiddleware<ServiceBusActorContextMiddleware>();
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
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            base.ConfigureContainer(container);

            container.Register<QueueSubscriber>(Lifestyle.Scoped);
            container.Register<CorrelationIdMiddleware>(Lifestyle.Scoped);
            container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            container.Register<EntryPointTelemetryScopeMiddleware>(Lifestyle.Scoped);
            container.Register<ServiceBusActorContextMiddleware>(Lifestyle.Scoped);
            container.Register<IActorContext, ActorContext>(Lifestyle.Scoped);
            container.Register<IActorProvider, ActorProvider>(Lifestyle.Scoped);
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
            container.Register<IProtobufMessageFactory, ProtobufMessageFactory>(Lifestyle.Singleton);
            container.Register<INotificationReceiver, NotificationReceiver>(Lifestyle.Scoped);
            container.Register<IntegrationEventReceiver>(Lifestyle.Scoped);
            container.Register<IActorMessageService, ActorMessageService>(Lifestyle.Scoped);
            container.Register<IMessageHubDispatcher, MessageHubDispatcher>(Lifestyle.Scoped);

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
