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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier
{
    [IntegrationTest]
    public sealed class RequestTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly IAccountingPointRepository _accountingPointRepository;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private readonly IConsumerRepository _consumerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly MarketRolesContext _context;
        private readonly IJsonSerializer _serializer;

        public RequestTests()
        {
            var services = new ServiceCollection();

            var connectionString = Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");
            services.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(connectionString, y => y.UseNodaTime()));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProviderStub>();
            services.AddScoped<IAccountingPointRepository, AccountingPointRepository>();
            services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();
            services.AddScoped<IConsumerRepository, ConsumerRepository>();
            services.AddScoped<IJsonSerializer, JsonSerializer>();
            services.AddScoped<IOutbox, OutboxProvider>();
            services.AddSingleton<IOutboxMessageFactory, OutboxMessageFactory>();

            services.AddMediatR(new[]
            {
                typeof(RequestChangeOfSupplierHandler).Assembly,
            });

            // Busines process responders
            services.AddScoped<IBusinessProcessResponder<RequestChangeOfSupplier>, RequestChangeOfSupplierResponder>();

            // Business process pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BusinessProcessResponderBehaviour<,>));

            _serviceProvider = services.BuildServiceProvider();
            _mediator = _serviceProvider.GetRequiredService<IMediator>();
            _accountingPointRepository = _serviceProvider.GetRequiredService<IAccountingPointRepository>();
            _energySupplierRepository = _serviceProvider.GetRequiredService<IEnergySupplierRepository>();
            _consumerRepository = _serviceProvider.GetRequiredService<IConsumerRepository>();
            _unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
            _context = _serviceProvider.GetRequiredService<MarketRolesContext>();
            _systemDateTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();
            _serializer = _serviceProvider.GetRequiredService<IJsonSerializer>();
        }

        [Fact]
        public async Task Request_WhenMeteringPointDoesNotExist_IsRejected()
        {
            var request = CreateRequest();

            var result = await _mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(request.MeteringPointId, publishedMessage.MeteringPoint);
        }

        [Fact]
        public async Task Request_WhenEnergySupplierIsUnknown_IsRejected()
        {
            CreateAccountingPoint();

            var request = CreateRequest();

            await _mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(request.MeteringPointId, publishedMessage.MeteringPoint);
        }

        //TODO: Fix in another PR.
        // [Fact]
        // public async Task Request_WhenInputValidationsAreBroken_IsRejected()
        // {
        //     // Arrange
        //     var energySupplierGlnNumber = "5790000555550";
        //     var meteringPointGsrnNumber = "571234567891234568";
        //     await Seed(energySupplierGlnNumber, meteringPointGsrnNumber).ConfigureAwait(false);
        //     var systemDateTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();
        //
        //     var command = new RequestChangeOfSupplier
        //     {
        //         MarketEvaluationPoint = new MarketEvaluationPoint(meteringPointGsrnNumber),
        //         EnergySupplier = new MarketParticipant(energySupplierGlnNumber),
        //         BalanceResponsibleParty = new MarketParticipant("2"),
        //         Consumer = new MarketParticipant("0101210000", null, null, "OOPS"), // A correct qualifier would be ARR or VA
        //         StartDate = systemDateTimeProvider.Now(),
        //     };
        //
        //     // Act
        //     await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);
        //
        //     // Assert (it's a rejected message)
        //     var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
        //     Assert.Equal(command.MarketEvaluationPoint.MRid, publishedMessage.MeteringPointId);
        // }
        [Fact]
        public async Task Request_WhenNoRulesAreBroken_IsSuccessful()
        {
            var accountingPoint = CreateAccountingPoint();
            var consumer = CreateConsumer();
            var supplier = CreateEnergySupplier();
            SetConsumerMovedIn(accountingPoint, consumer.ConsumerId, supplier.EnergySupplierId);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var request = CreateRequest();

            await _mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierApproved>().ConfigureAwait(false);
            Assert.Equal(request.MeteringPointId, publishedMessage.MeteringPointId);
        }

        public void Dispose()
        {
            CleanupDatabase();
        }

        private async Task<TMessage> GetLastMessageFromOutboxAsync<TMessage>()
        {
            var outboxMessage = await _context.OutboxMessages.FirstAsync().ConfigureAwait(false);
            var @event = _serializer.Deserialize<TMessage>(outboxMessage.Data);
            return @event;
        }

        private void CleanupDatabase()
        {
            var cleanupStatement = $"DELETE FROM [dbo].[ConsumerRegistrations] " +
                                   $"DELETE FROM [dbo].[SupplierRegistrations] " +
                                   $"DELETE FROM [dbo].[BusinessProcesses] " +
                                   $"DELETE FROM [dbo].[Consumers] " +
                                   $"DELETE FROM [dbo].[EnergySuppliers] " +
                                   $"DELETE FROM [dbo].[AccountingPoints] " +
                                   $"DELETE FROM [dbo].[OutboxMessages]";

            _context.Database.ExecuteSqlRaw(cleanupStatement);
            _context.Dispose();
        }

        private RequestChangeOfSupplier CreateRequest()
        {
            return new RequestChangeOfSupplier(
                TransactionId: Guid.NewGuid().ToString(),
                EnergySupplierId: SampleData.SampleGlnNumber,
                ConsumerId: SampleData.SampleConsumerId,
                MeteringPointId: SampleData.SampleGsrnNumber,
                StartDate: _systemDateTimeProvider.Now());
        }

        private Consumer CreateConsumer()
        {
            var consumerId = new ConsumerId(Guid.NewGuid());
            var consumer = new Consumer(consumerId, CprNumber.Create(SampleData.SampleConsumerId));

            _consumerRepository.Add(consumer);

            return consumer;
        }

        private EnergySupplier CreateEnergySupplier()
        {
            var energySupplierId = new EnergySupplierId(Guid.NewGuid());
            var energySupplierGln = new GlnNumber(SampleData.SampleGlnNumber);
            var energySupplier = new EnergySupplier(energySupplierId, energySupplierGln);
            _energySupplierRepository.Add(energySupplier);
            return energySupplier;
        }

        private AccountingPoint CreateAccountingPoint()
        {
            var meteringPoint =
                AccountingPoint.CreateProduction(
                    GsrnNumber.Create(SampleData.SampleGsrnNumber), true);

            _accountingPointRepository.Add(meteringPoint);

            return meteringPoint;
        }

        private void SetConsumerMovedIn(AccountingPoint accountingPoint, ConsumerId consumerId, EnergySupplierId energySupplierId)
        {
            var systemTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();
            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var transaction = new Transaction(Guid.NewGuid().ToString());

            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, systemTimeProvider);
        }
    }
}
