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
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.IntegrationEventDispatching.MoveIn;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.MoveIn
{
    #pragma warning disable
    public class MoveInTests : TestHost
    {
        private readonly AccountingPoint _accountingPoint;
        private readonly EnergySupplier _energySupplier;

        public MoveInTests()
        {
            _accountingPoint = CreateAccountingPoint();
            _energySupplier = CreateEnergySupplier();
            SaveChanges();
        }

        [Fact]
        public async Task Accept_WhenConsumerIsRegistered_IntegrationEventIsPublished()
        {
            var consumerSsn = SampleData.ConsumerSSN;
            var moveInDate = GetService<ISystemDateTimeProvider>().Now();
            var request = new RequestMoveIn(
                SampleData.Transaction,
                SampleData.GlnNumber,
                consumerSsn,
                string.Empty,
                SampleData.ConsumerName,
                SampleData.GsrnNumber,
                moveInDate);

            var result = await SendRequest(request) as BusinessProcessResult;
            var publishedMessage = await GetLastMessageFromOutboxAsync<ConsumerRegisteredIntegrationEvent>().ConfigureAwait(false);

            Assert.True(result.Success);
            Assert.NotNull(publishedMessage);
        }
    }
}
