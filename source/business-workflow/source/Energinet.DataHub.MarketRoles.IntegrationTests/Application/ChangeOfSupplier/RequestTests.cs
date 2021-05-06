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
    public sealed class RequestTests : TestHost, IDisposable
    {
        [Fact]
        public async Task Request_WhenMeteringPointDoesNotExist_IsRejected()
        {
            var request = CreateRequest();

            var result = await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierRejected>().ConfigureAwait(false);
            Assert.Equal(request.MeteringPointId, publishedMessage.MeteringPoint);
        }

        [Fact]
        public async Task Request_WhenEnergySupplierIsUnknown_IsRejected()
        {
            CreateAccountingPoint();

            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

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
            await MarketRolesContext.SaveChangesAsync().ConfigureAwait(false);

            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            var publishedMessage = await GetLastMessageFromOutboxAsync<RequestChangeOfSupplierApproved>().ConfigureAwait(false);
            Assert.Equal(request.MeteringPointId, publishedMessage.MeteringPointId);
        }

        private async Task<TMessage> GetLastMessageFromOutboxAsync<TMessage>()
        {
            var outboxMessage = await MarketRolesContext.OutboxMessages.FirstAsync().ConfigureAwait(false);
            var @event = Serializer.Deserialize<TMessage>(outboxMessage.Data);
            return @event;
        }

        private RequestChangeOfSupplier CreateRequest()
        {
            return new RequestChangeOfSupplier(
                TransactionId: Guid.NewGuid().ToString(),
                EnergySupplierId: SampleData.SampleGlnNumber,
                ConsumerId: SampleData.SampleConsumerId,
                MeteringPointId: SampleData.SampleGsrnNumber,
                StartDate: SystemDateTimeProvider.Now());
        }
    }
}
