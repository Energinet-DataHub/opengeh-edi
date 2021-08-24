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

using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Application.MoveIn.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration.IntegrationEvents.EnergySupplierChange;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.MoveIn
{
    [IntegrationTest]
    public class EffectuateTests : TestHost
    {
        [Fact]
        public async Task Effectuate_WhenEffectiveDateIsDue_IntegrationEventsArePublished()
        {
            var (accountingPoint, transaction) = await SetupScenarioAsync().ConfigureAwait(false);
            var command = new EffectuateConsumerMoveIn(accountingPoint.Id.Value, transaction.Value);

            await InvokeCommandAsync(command).ConfigureAwait(false);

            AssertOutboxMessage<EnergySupplierChangedIntegrationEvent>();
        }

        private async Task<(AccountingPoint AccountingPoint, Transaction Transaction)> SetupScenarioAsync()
        {
            var accountingPoint = CreateAccountingPoint();
            CreateEnergySupplier();
            SaveChanges();

            var requestMoveIn = new RequestMoveIn(
                SampleData.Transaction,
                SampleData.GlnNumber,
                SampleData.ConsumerSSN,
                string.Empty,
                SampleData.ConsumerName,
                SampleData.GsrnNumber,
                SampleData.MoveInDate);

            var result = await SendRequestAsync(requestMoveIn).ConfigureAwait(false);

            return (accountingPoint, Transaction.Create(result.TransactionId));
        }
    }
}
