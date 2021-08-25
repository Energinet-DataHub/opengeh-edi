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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Application.MoveIn.Validation;
using Energinet.DataHub.MarketRoles.Contracts;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.EntryPoints.Common.MediatR;
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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Xunit;
using RequestChangeOfSupplier = Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.RequestChangeOfSupplier;
using RequestMoveIn = Energinet.DataHub.MarketRoles.Application.MoveIn.RequestMoveIn;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application
{
    [Collection("IntegrationTest")]
#pragma warning disable CA1724 // TODO: TestHost is reserved. Maybe refactor to base EntryPoint?
    public class TestHost : IDisposable
    {
        private readonly Scope _scope;
        private readonly Container _container;
        private bool _disposed;
        private SqlConnection? _sqlConnection;
        private BusinessProcessId? _businessProcessId;
        private string _connectionString;

        protected TestHost(DatabaseFixture databaseFixture)
        {
            if (databaseFixture == null) throw new ArgumentNullException(nameof(databaseFixture));

            _connectionString = databaseFixture.GetConnectionString.Value;

            _container = new Container();
            var serviceCollection = new ServiceCollection();

            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            _container.SendProtobuf<MarketRolesEnvelope>();
            _container.ReceiveProtobuf<MarketRolesEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.MarketRolesMessagesCase)
                    .WithParser(() => MarketRolesEnvelope.Parser));

            serviceCollection.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(_connectionString, y => y.UseNodaTime()));
            serviceCollection.AddSimpleInjector(_container);
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider().UseSimpleInjector(_container);

            _container.Register<IUnitOfWork, UnitOfWork>(Lifestyle.Scoped);
            _container.Register<IAccountingPointRepository, AccountingPointRepository>(Lifestyle.Scoped);
            _container.Register<IEnergySupplierRepository, EnergySupplierRepository>(Lifestyle.Scoped);
            _container.Register<IProcessManagerRepository, ProcessManagerRepository>(Lifestyle.Scoped);
            _container.Register<IConsumerRepository, ConsumerRepository>(Lifestyle.Scoped);
            _container.Register<IOutbox, OutboxProvider>(Lifestyle.Scoped);
            _container.Register<IOutboxManager, OutboxManager>(Lifestyle.Scoped);
            _container.Register<IOutboxMessageFactory, OutboxMessageFactory>(Lifestyle.Singleton);
            _container.Register<IJsonSerializer, JsonSerializer>(Lifestyle.Singleton);
            _container.Register<ISystemDateTimeProvider, SystemDateTimeProviderStub>(Lifestyle.Singleton);
            _container.Register<IDomainEventsAccessor, DomainEventsAccessor>();
            _container.Register<IDomainEventsDispatcher, DomainEventsDispatcher>();
            _container.Register<IDomainEventPublisher, DomainEventPublisher>();
            _container.Register<ICommandScheduler, CommandScheduler>(Lifestyle.Scoped);
            _container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(_connectionString));
            _container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);

            // Business process responders
            _container.Register<IBusinessProcessResultHandler<RequestChangeOfSupplier>, RequestChangeOfSupplierResultHandler>(Lifestyle.Scoped);
            _container.Register<IBusinessProcessResultHandler<RequestMoveIn>, RequestMoveInResultHandler>(Lifestyle.Scoped);

            // Input validation(
            _container.Register<IValidator<RequestChangeOfSupplier>, RequestChangeOfSupplierRuleSet>(Lifestyle.Scoped);
            _container.Register<IValidator<RequestMoveIn>, RequestMoveInRuleSet>(Lifestyle.Scoped);
            _container.AddValidationErrorConversion(
                validateRegistrations: true,
                typeof(RequestMoveIn).Assembly, // Application
                typeof(ConsumerMovedIn).Assembly, // Domain
                typeof(ErrorMessageFactory).Assembly); // Infrastructure

            // Actor Notification handlers
            _container.Register<IEndOfSupplyNotifier, EndOfSupplyNotifier>(Lifestyle.Scoped);
            _container.Register<IConsumerDetailsForwarder, ConsumerDetailsForwarder>(Lifestyle.Scoped);
            _container.Register<IMeteringPointDetailsForwarder, MeteringPointDetailsForwarder>(Lifestyle.Scoped);

            _container.BuildMediator(
                new[]
                {
                    typeof(RequestChangeOfSupplierHandler).Assembly,
                    typeof(PublishWhenEnergySupplierHasChanged).Assembly,
                },
                new[]
                {
                    typeof(UnitOfWorkBehaviour<,>),
                    typeof(InputValidationBehaviour<,>),
                    typeof(BusinessProcessResponderBehaviour<,>),
                    typeof(DomainEventsDispatcherBehaviour<,>),
                    typeof(InternalCommandHandlingBehaviour<,>),
                });

            _container.Verify();

            _scope = AsyncScopedLifestyle.BeginScope(_container);

            _container.GetInstance<ICorrelationContext>().SetCorrelationId(Guid.NewGuid().ToString());

            CleanupDatabase();

            ServiceProvider = serviceProvider;
            Mediator = _container.GetInstance<IMediator>();
            AccountingPointRepository = _container.GetInstance<IAccountingPointRepository>();
            EnergySupplierRepository = _container.GetInstance<IEnergySupplierRepository>();
            ProcessManagerRepository = _container.GetInstance<IProcessManagerRepository>();
            ConsumerRepository = _container.GetInstance<IConsumerRepository>();
            UnitOfWork = _container.GetInstance<IUnitOfWork>();
            MarketRolesContext = _container.GetInstance<MarketRolesContext>();
            SystemDateTimeProvider = _container.GetInstance<ISystemDateTimeProvider>();
            Serializer = _container.GetInstance<IJsonSerializer>();
            CommandScheduler = _container.GetInstance<ICommandScheduler>();
            Transaction = new Transaction(Guid.NewGuid().ToString());
        }

        // TODO: Get rid of all properties and methods instead
        protected IServiceProvider ServiceProvider { get; }

        protected IMediator Mediator { get; }

        protected IAccountingPointRepository AccountingPointRepository { get; }

        protected IEnergySupplierRepository EnergySupplierRepository { get; }

        protected IConsumerRepository ConsumerRepository { get; }

        protected IProcessManagerRepository ProcessManagerRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected ISystemDateTimeProvider SystemDateTimeProvider { get; }

        protected MarketRolesContext MarketRolesContext { get; }

        protected ICommandScheduler CommandScheduler { get; }

        protected IJsonSerializer Serializer { get; }

        protected Transaction Transaction { get; }

        protected Instant EffectiveDate => SystemDateTimeProvider.Now();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static Transaction CreateTransaction()
        {
            return new Transaction(Guid.NewGuid().ToString());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            CleanupDatabase();

            _sqlConnection?.Dispose();
            _scope.Dispose();
            _container.Dispose();

            _disposed = true;
        }

        protected TService GetService<TService>()
            where TService : class
        {
            return _container.GetInstance<TService>();
        }

        protected SqlConnection GetSqlDbConnection()
        {
            if (_sqlConnection is null) _sqlConnection = new SqlConnection(_connectionString);
            if (_sqlConnection.State == ConnectionState.Closed) _sqlConnection.Open();
            return _sqlConnection;
        }

        protected void SaveChanges()
        {
            GetService<MarketRolesContext>().SaveChanges();
        }

        protected async Task<BusinessProcessResult> SendRequestAsync(IBusinessRequest request)
        {
            var result = await GetService<IMediator>().Send(request, CancellationToken.None).ConfigureAwait(false);
            return result;
        }

        protected Task InvokeCommandAsync(InternalCommand command)
        {
            return GetService<IMediator>().Send(command, CancellationToken.None);
        }

        protected async Task<TCommand?> GetEnqueuedCommandAsync<TCommand>()
        {
            var businessProcessId = _businessProcessId?.Value ?? throw new InvalidOperationException("Unknown BusinessProcessId");
            var type = typeof(TCommand).FullName;
            var queuedCommand = MarketRolesContext.QueuedInternalCommands
                .FirstOrDefault(queuedInternalCommand =>
                    queuedInternalCommand.BusinessProcessId.Equals(businessProcessId) &&
#pragma warning disable CA1309 // Warns about: "Use ordinal string comparison", but we want EF to take care of this.
                    queuedInternalCommand.Type.Equals(type));

            if (queuedCommand is null)
            {
                return default(TCommand);
            }

            var messageExtractor = GetService<MessageExtractor>();
            var command = await messageExtractor.ExtractAsync(queuedCommand!.Data).ConfigureAwait(false);
            return (TCommand)command;
        }

        protected async Task<TCommand?> GetEnqueuedCommandAsync<TCommand>(BusinessProcessId businessProcessId)
        {
            var type = typeof(TCommand).FullName;
            var queuedCommand = MarketRolesContext.QueuedInternalCommands
                .FirstOrDefault(queuedInternalCommand =>
                    queuedInternalCommand.BusinessProcessId.Equals(businessProcessId.Value) &&
#pragma warning disable CA1309 // Warns about: "Use ordinal string comparison", but we want EF to take care of this.
                    queuedInternalCommand.Type.Equals(type));

            if (queuedCommand is null)
            {
                return default(TCommand);
            }

            var messageExtractor = GetService<MessageExtractor>();
            var command = await messageExtractor.ExtractAsync(queuedCommand!.Data).ConfigureAwait(false);
            return (TCommand)command;
        }

        protected Consumer CreateConsumer()
        {
            var consumerId = new ConsumerId(Guid.NewGuid());
            var consumer = new Consumer(consumerId, CprNumber.Create(SampleData.ConsumerSSN), ConsumerName.Create(SampleData.ConsumerName));

            ConsumerRepository.Add(consumer);

            return consumer;
        }

        protected EnergySupplier CreateEnergySupplier()
        {
            var energySupplierId = new EnergySupplierId(Guid.NewGuid());
            var energySupplierGln = new GlnNumber(SampleData.GlnNumber);
            var energySupplier = new EnergySupplier(energySupplierId, energySupplierGln);
            EnergySupplierRepository.Add(energySupplier);
            return energySupplier;
        }

        protected AccountingPoint CreateAccountingPoint()
        {
            var meteringPoint =
                AccountingPoint.CreateProduction(
                    GsrnNumber.Create(SampleData.GsrnNumber), true);

            AccountingPointRepository.Add(meteringPoint);

            return meteringPoint;
        }

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId)
        {
            var systemTimeProvider = GetService<ISystemDateTimeProvider>();
            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var transaction = CreateTransaction();
            SetConsumerMovedIn(accountingPoint, consumerId, energySupplierId, moveInDate, transaction);
        }

        protected void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId, Instant moveInDate, Transaction transaction)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));

            var systemTimeProvider = GetService<ISystemDateTimeProvider>();
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, systemTimeProvider);
        }

        protected void RegisterChangeOfSupplier(AccountingPoint accountingPoint, EnergySupplierId energySupplierId, Transaction transaction)
        {
            if (accountingPoint == null) throw new ArgumentNullException(nameof(accountingPoint));

            var systemTimeProvider = GetService<ISystemDateTimeProvider>();

            var changeSupplierDate = systemTimeProvider.Now();

            accountingPoint.AcceptChangeOfSupplier(energySupplierId, changeSupplierDate, transaction, systemTimeProvider);
        }

        protected BusinessProcessId GetBusinessProcessId(Transaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            if (_businessProcessId == null)
            {
                using var connection = new SqlConnection(_connectionString);
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                using var command = new SqlCommand($"SELECT Id FROM [dbo].[BusinessProcesses] WHERE TransactionId = @transaction", connection);
                command.Parameters.Add("@transaction", SqlDbType.NVarChar).Value = transaction.Value;
                var id = (Guid)command.ExecuteScalar();
                _businessProcessId = BusinessProcessId.Create(id);
            }

            return _businessProcessId;
        }

        protected IEnumerable<TMessage> GetOutboxMessages<TMessage>()
        {
            var jsonSerializer = GetService<IJsonSerializer>();
            var context = GetService<MarketRolesContext>();
            return context.OutboxMessages
                .Where(message => message.Type == typeof(TMessage).FullName)
                .Select(message => jsonSerializer.Deserialize<TMessage>(message.Data));
        }

        protected void AssertOutboxMessage<TMessage>(Func<TMessage, bool> funcAssert, int count = 1)
        {
            if (funcAssert == null) throw new ArgumentNullException(nameof(funcAssert));

            var messages = GetOutboxMessages<TMessage>().Where(funcAssert.Invoke);

            messages.Should().HaveCount(count);
            messages.Should().NotContainNulls();
            messages.Should().AllBeOfType<TMessage>();
        }

        protected void AssertOutboxMessage<TMessage>()
        {
            var message = GetOutboxMessages<TMessage>().SingleOrDefault();

            message.Should().NotBeNull();
            message.Should().BeOfType<TMessage>();
        }

        private void CleanupDatabase()
        {
            var cleanupStatement = $"DELETE FROM [dbo].[ConsumerRegistrations] " +
                                   $"DELETE FROM [dbo].[SupplierRegistrations] " +
                                   $"DELETE FROM [dbo].[ProcessManagers] " +
                                   $"DELETE FROM [dbo].[BusinessProcesses] " +
                                   $"DELETE FROM [dbo].[Consumers] " +
                                   $"DELETE FROM [dbo].[EnergySuppliers] " +
                                   $"DELETE FROM [dbo].[AccountingPoints] " +
                                   $"DELETE FROM [dbo].[OutboxMessages] " +
                                   $"DELETE FROM [dbo].[QueuedInternalCommands]";

            using var sqlCommand = new SqlCommand(cleanupStatement, GetSqlDbConnection());
            sqlCommand.ExecuteNonQuery();
        }
    }
}
