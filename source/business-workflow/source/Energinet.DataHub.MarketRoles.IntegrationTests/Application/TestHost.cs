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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.DomainEvents;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Application.Integration;
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
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.Errors;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEventDispatching.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.Services;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf.Integration;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
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
    public class TestHost : IDisposable
    {
        private readonly Scope _scope;
        private readonly Container _container;
        private readonly IServiceProvider _serviceProvider;
        private SqlConnection _sqlConnection = null;
        private BusinessProcessId _businessProcessId = null;

        protected TestHost()
        {
            _container = new Container();
            var serviceCollection = new ServiceCollection();

            serviceCollection.SendProtobuf<MarketRolesEnvelope>();
            serviceCollection.ReceiveProtobuf<MarketRolesEnvelope>(
                config => config
                    .FromOneOf(envelope => envelope.MarketRolesMessagesCase)
                    .WithParser(() => MarketRolesEnvelope.Parser));

            serviceCollection.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(ConnectionString, y => y.UseNodaTime()));
            serviceCollection.AddSimpleInjector(_container);
            _serviceProvider = serviceCollection.BuildServiceProvider().UseSimpleInjector(_container);

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
            _container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(ConnectionString));
            _container.Register<ICorrelationContext, CorrelationContext>(Lifestyle.Scoped);
            _container.Register<AcknowledgementXmlSerializer>(Lifestyle.Scoped);

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

            ServiceProvider = _serviceProvider;
            Mediator = _container.GetService<IMediator>();
            AccountingPointRepository = _container.GetService<IAccountingPointRepository>();
            EnergySupplierRepository = _container.GetService<IEnergySupplierRepository>();
            ProcessManagerRepository = _container.GetService<IProcessManagerRepository>();
            ConsumerRepository = _container.GetService<IConsumerRepository>();
            UnitOfWork = _container.GetService<IUnitOfWork>();
            MarketRolesContext = _container.GetService<MarketRolesContext>();
            SystemDateTimeProvider = _container.GetService<ISystemDateTimeProvider>();
            Serializer = _container.GetService<IJsonSerializer>();
            CommandScheduler = _container.GetService<ICommandScheduler>();
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

        private string ConnectionString =>
            Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");

        public void Dispose()
        {
            CleanupDatabase();
        }

        protected TService GetService<TService>()
        {
            return _container.GetService<TService>();
        }

        protected SqlConnection GetSqlDbConnection()
        {
            if (_sqlConnection is null)
                _sqlConnection = new SqlConnection(ConnectionString);

            if (_sqlConnection.State == ConnectionState.Closed)
                _sqlConnection.Open();
            return _sqlConnection;
        }

        protected void SaveChanges()
        {
            GetService<MarketRolesContext>().SaveChanges();
        }

        protected async Task<TMessage> GetLastMessageFromOutboxAsync<TMessage>()
        {
            var outboxMessage = await MarketRolesContext.OutboxMessages.FirstAsync(m => m.Type.Equals(typeof(TMessage).FullName)).ConfigureAwait(false);
            var @event = Serializer.Deserialize<TMessage>(outboxMessage.Data);
            return @event;
        }

        protected async Task<BusinessProcessResult> SendRequest(IBusinessRequest request)
        {
            var result = await GetService<IMediator>().Send(request, CancellationToken.None);
            return result;
        }

        protected Task InvokeCommandAsync(InternalCommand command)
        {
            return GetService<IMediator>().Send(command, CancellationToken.None);
        }

        protected async Task<TCommand> GetEnqueuedCommandAsync<TCommand>()
        {
            var type = typeof(TCommand).FullName;
            var queuedCommand = MarketRolesContext.QueuedInternalCommands
                .FirstOrDefault(queuedInternalCommand =>
                    queuedInternalCommand.BusinessProcessId.Equals(_businessProcessId.Value) &&
                    queuedInternalCommand.Type.Equals(type));

            if (queuedCommand is null)
            {
                return default(TCommand);
            }

            var messageExtractor = GetService<MessageExtractor>();
            var command = await messageExtractor.ExtractAsync(queuedCommand!.Data).ConfigureAwait(false);
            return (TCommand)command;
        }

        protected async Task<TCommand> GetEnqueuedCommandAsync<TCommand>(BusinessProcessId businessProcessId)
        {
            var type = typeof(TCommand).FullName;
            var queuedCommand = MarketRolesContext.QueuedInternalCommands
                .FirstOrDefault(queuedInternalCommand =>
                    queuedInternalCommand.BusinessProcessId.Equals(businessProcessId.Value) &&
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

        protected Transaction CreateTransaction()
        {
            return new Transaction(Guid.NewGuid().ToString());
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
            var systemTimeProvider = GetService<ISystemDateTimeProvider>();
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, systemTimeProvider);
        }

        protected void RegisterChangeOfSupplier(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId, Transaction transaction)
        {
            var systemTimeProvider = GetService<ISystemDateTimeProvider>();

            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var changeSupplierDate = systemTimeProvider.Now();

            SetConsumerMovedIn(accountingPoint, consumerId, energySupplierId);
            accountingPoint.AcceptChangeOfSupplier(energySupplierId, changeSupplierDate, transaction, systemTimeProvider);
        }

        protected BusinessProcessId GetBusinessProcessId()
        {
            if (_businessProcessId == null)
            {
                var command = new SqlCommand($"SELECT Id FROM [dbo].[BusinessProcesses] WHERE TransactionId = '{Transaction.Value}'", GetSqlDbConnection());
                var id = command.ExecuteScalar();
                _businessProcessId = new BusinessProcessId(Guid.Parse(id.ToString()));
            }

            return _businessProcessId;
        }

        protected BusinessProcessId GetBusinessProcessId(Transaction transaction)
        {
            if (_businessProcessId == null)
            {
                var connection = new SqlConnection(ConnectionString);
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                var command = new SqlCommand($"SELECT Id FROM [dbo].[BusinessProcesses] WHERE TransactionId = '{transaction.Value}'", connection);
                var id = command.ExecuteScalar();
                _businessProcessId = new BusinessProcessId(Guid.Parse(id.ToString()));
            }

            return _businessProcessId;
        }

        protected async Task AssertOutboxMessageAsync<TMessage>(Func<TMessage, bool> funcAssert)
        {
            var publishedMessage = await GetLastMessageFromOutboxAsync<TMessage>().ConfigureAwait(false);
            var assertion = funcAssert.Invoke(publishedMessage);

            Assert.NotNull(publishedMessage);
            Assert.True(assertion);
        }

        protected async Task AssertOutboxMessageAsync<TMessage>()
        {
            var publishedMessage = await GetLastMessageFromOutboxAsync<TMessage>().ConfigureAwait(false);
            Assert.NotNull(publishedMessage);
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

            new SqlCommand(cleanupStatement, GetSqlDbConnection()).ExecuteNonQuery();
        }
    }
}
