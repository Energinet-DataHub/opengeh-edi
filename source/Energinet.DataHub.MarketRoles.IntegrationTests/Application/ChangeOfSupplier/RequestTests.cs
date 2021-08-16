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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI.ChangeOfSupplier;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier
{
    [IntegrationTest]
    public sealed class RequestTests : TestHost
    {
        [Fact]
        public async Task Request_WhenMeteringPointDoesNotExist_IsRejected()
        {
            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType == nameof(RequestChangeOfSupplierRejected))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Request_WhenEnergySupplierIsUnknown_IsRejected()
        {
            CreateAccountingPoint();

            var request = CreateRequest();

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType == nameof(RequestChangeOfSupplierRejected))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task Request_WhenInputValidationsAreBroken_IsRejected()
        {
            var request = CreateRequest(
                SampleData.Transaction,
                SampleData.GlnNumber,
                SampleData.ConsumerSSN,
                "THIS_IS_NOT_VALID_GSRN_NUMBER",
                SampleData.MoveInDate);

            await Mediator.Send(request, CancellationToken.None).ConfigureAwait(false);

            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType == nameof(RequestChangeOfSupplierRejected))
                .ConfigureAwait(false);
        }

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
            await AssertOutboxMessageAsync<PostOfficeEnvelope>(envelope => envelope.MessageType == nameof(RequestChangeOfSupplierApproved))
                .ConfigureAwait(false);
        }

        private static RequestChangeOfSupplier CreateRequest(string transaction, string energySupplierGln, string consumerId, string gsrnNumber, string startDate)
        {
            return new RequestChangeOfSupplier(
                TransactionId: transaction,
                EnergySupplierGlnNumber: energySupplierGln,
                SocialSecurityNumber: consumerId,
                AccountingPointGsrnNumber: gsrnNumber,
                StartDate: startDate);
        }

        private static RequestChangeOfSupplier CreateRequest()
        {
            return CreateRequest(
                SampleData.Transaction,
                SampleData.GlnNumber,
                SampleData.ConsumerSSN,
                SampleData.GsrnNumber,
                Instant.FromDateTimeUtc(DateTime.UtcNow.AddHours(1)).ToString());
        }
    }
}
