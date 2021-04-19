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
using System.Data.SqlClient;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dapper.NodaTime;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.ActorMessages;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.ActorMessages;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.EnergySuppliers;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence.MarketEvaluationPoints;
using Energinet.DataHub.MarketData.Infrastructure.IntegrationEvents;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using Energinet.DataHub.MarketData.Infrastructure.UseCaseProcessing;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Messaging.MessageTypes.Common;
using GreenEnergyHub.TestHelpers.Traits;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using MarketEvaluationPoint = GreenEnergyHub.Messaging.MessageTypes.Common.MarketEvaluationPoint;

namespace Energinet.DataHub.MarketData.IntegrationTests.Application.ChangeOfSupplier
{
    [Trait(TraitNames.Category, TraitValues.IntegrationTest)]
    public sealed class RequestChangeOfSupplierTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly IMeteringPointRepository _meteringPointRepository;
        private readonly IEnergySupplierRepository _energySupplierRepository;
        private readonly IUnitOfWorkCallback _unitOfWorkCallback;
        private readonly IActorMessagePublisher _actorMessagePublisher;

        public RequestChangeOfSupplierTests()
        {
            var services = new ServiceCollection();

            _connectionString =
                Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");

            services.AddScoped<IDbConnectionFactory>(sp => new SqlDbConnectionFactory(_connectionString));
            services.AddScoped<IUnitOfWorkCallback, UnitOfWorkCallback>();
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProviderStub>();
            services.AddScoped<IEventPublisher, EventPublisherStub>();
            services.AddScoped<IActorMessagePublisher, ActorMessagePublisher>();
            services.AddScoped<IMeteringPointRepository, MeteringPointRepository>();
            services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();

            services.AddMediatR(new[]
            {
                typeof(RequestChangeSupplierCommandHandler).Assembly,
            });

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkHandlerBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PublishIntegrationEventsHandlerBehavior<,>));
            services.AddScoped<IPipelineBehavior<RequestChangeOfSupplier, RequestChangeOfSupplierResult>, PublishActorMessageHandlerBehavior>();

            services.AddGreenEnergyHub(typeof(RequestChangeOfSupplier).Assembly);

            DapperNodaTimeSetup.Register();

            _serviceProvider = services.BuildServiceProvider();
            _mediator = _serviceProvider.GetRequiredService<IMediator>();
            _meteringPointRepository = _serviceProvider.GetRequiredService<IMeteringPointRepository>();
            _energySupplierRepository = _serviceProvider.GetRequiredService<IEnergySupplierRepository>();
            _unitOfWorkCallback = _serviceProvider.GetRequiredService<IUnitOfWorkCallback>();
            _actorMessagePublisher = _serviceProvider.GetRequiredService<IActorMessagePublisher>();
        }

        [Fact]
        public async Task Request_WhenMeteringPointDoesNotExist_IsRejected()
        {
            var energySupplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";
            await Seed(energySupplierGlnNumber, meteringPointGsrnNumber).ConfigureAwait(false);

            var command = new RequestChangeOfSupplier()
            {
                MarketEvaluationPoint = new MarketEvaluationPoint(meteringPointGsrnNumber),
                EnergySupplier = new MarketParticipant(energySupplierGlnNumber),
            };

            var result = await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(command.MarketEvaluationPoint.MRid, publishedMessage.MeteringPointId);
        }

        [Fact]
        public async Task Request_WhenEnergySupplierIsUnknown_IsRejected()
        {
            var energySupplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";
            await Seed(energySupplierGlnNumber, meteringPointGsrnNumber).ConfigureAwait(false);

            var command = new RequestChangeOfSupplier()
            {
                MarketEvaluationPoint = new MarketEvaluationPoint(meteringPointGsrnNumber),
                EnergySupplier = new MarketParticipant(energySupplierGlnNumber),
            };

            await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(command.MarketEvaluationPoint.MRid, publishedMessage.MeteringPointId);
        }

        [Fact]
        public async Task Request_WhenInputValidationsAreBroken_IsRejected()
        {
            // Arrange
            var energySupplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";
            await Seed(energySupplierGlnNumber, meteringPointGsrnNumber).ConfigureAwait(false);
            var systemDateTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();

            var command = new RequestChangeOfSupplier
            {
                MarketEvaluationPoint = new MarketEvaluationPoint(meteringPointGsrnNumber),
                EnergySupplier = new MarketParticipant(energySupplierGlnNumber),
                BalanceResponsibleParty = new MarketParticipant("2"),
                Consumer = new MarketParticipant("0101210000", null, null, "OOPS"), // A correct qualifier would be ARR or VA
                StartDate = systemDateTimeProvider.Now(),
            };

            // Act
            await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);

            // Assert (it's a rejected message)
            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(command.MarketEvaluationPoint.MRid, publishedMessage.MeteringPointId);
        }

        [Fact]
        public async Task Request_WhenNoRulesAreBroken_IsSuccessful()
        {
            var energySupplierGlnNumber = "5790000555550";
            var meteringPointGsrnNumber = "571234567891234568";
            await Seed(energySupplierGlnNumber, meteringPointGsrnNumber).ConfigureAwait(false);

            var systemDateTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();
            var command = new RequestChangeOfSupplier()
            {
                MarketEvaluationPoint = new MarketEvaluationPoint(meteringPointGsrnNumber),
                EnergySupplier = new MarketParticipant(energySupplierGlnNumber),
                BalanceResponsibleParty = new MarketParticipant("2"),
                Consumer = new MarketParticipant("0101210000", null, null, "ARR"),
                StartDate = systemDateTimeProvider.Now(),
            };

            await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierApproved>().ConfigureAwait(false);
            Assert.Equal(command.MarketEvaluationPoint.MRid, publishedMessage.MeteringPointId);
        }

        public void Dispose()
        {
            CleanupDatabase();
        }

        private async Task<TMessage> GetLastMessageFromOutboxAsync<TMessage>()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = $"SELECT * FROM [dbo].[OutgoingActorMessages]";
                var outboxmessage = await connection.QuerySingleAsync<OutgoingActorMessage>(query).ConfigureAwait(false);

                var serializer = new GreenEnergyHub.Json.JsonSerializer();
                var @event = serializer.Deserialize<TMessage>(outboxmessage.Data);
                return (TMessage)@event;
            }
        }

        private void CleanupDatabase()
        {
            var cleanupStatement = $"DELETE FROM [dbo].[Relationships] " +
                                        $"DELETE FROM [dbo].[MarketParticipants] " +
                                        $"DELETE FROM [dbo].[MarketEvaluationPoints] " +
                                        $"DELETE FROM [dbo].[OutgoingActorMessages]";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(cleanupStatement);
            }
        }

        private async Task Seed(string energySupplierGlnNumber, string meteringPointGsrnNumber)
        {
            //TODO: Need to separate customers from energy suppliers - This does not make any sense at all
            var customerId = "Unknown";
            var customer = new EnergySupplier(new GlnNumber(customerId));
            _energySupplierRepository.Add(customer);

            var energySupplierGln = new GlnNumber(energySupplierGlnNumber);
            var energySupplier = new EnergySupplier(energySupplierGln);
            _energySupplierRepository.Add(energySupplier);

            await _unitOfWorkCallback.CommitAsync().ConfigureAwait(false);

            var meteringPoint =
                AccountingPoint.CreateProduction(
                    GsrnNumber.Create(meteringPointGsrnNumber), true);

            var systemTimeProvider = _serviceProvider.GetRequiredService<ISystemDateTimeProvider>();

            var consumerId = new ConsumerId(customerId);
            var moveInDate = systemTimeProvider.Now().Minus(Duration.FromDays(365));
            var processId = new ProcessId(Guid.NewGuid().ToString());
            meteringPoint.AcceptConsumerMoveIn(consumerId, new EnergySupplierId(energySupplierGlnNumber), moveInDate, processId);
            meteringPoint.EffectuateConsumerMoveIn(processId, systemTimeProvider);
            _meteringPointRepository.Add(meteringPoint);
            await _unitOfWorkCallback.CommitAsync().ConfigureAwait(false);
        }
    }
}
