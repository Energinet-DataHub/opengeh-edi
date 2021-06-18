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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints.MoveIn
{
    [UnitTest]
    public class EffectuateTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider = new SystemDateTimeProviderStub();

        public EffectuateTests()
        {
            _systemDateTimeProvider.SetNow(Instant.FromUtc(2020, 1, 1, 0, 0));
        }

        [Fact]
        public void Effectuate_WhenAheadOfEffectiveDate_IsNotPossible()
        {
            var (accountingPoint, consumerId, energySupplierId, transaction) = CreateTestValues();
            var moveInDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(1));
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, transaction);

            Assert.Throws<BusinessProcessException>(() =>
                accountingPoint.EffectuateConsumerMoveIn(transaction, _systemDateTimeProvider));
        }

        [Fact]
        public void Effectuate_WhenProcessIdDoesNotExists_IsNotPossible()
        {
            var (accountingPoint, _, _, _) = CreateTestValues();
            var nonExistingProcessId = new Transaction("NonExisting");

            Assert.Throws<BusinessProcessException>(() =>
                accountingPoint.EffectuateConsumerMoveIn(nonExistingProcessId, _systemDateTimeProvider));
        }

        private AccountingPoint Create()
        {
            var gsrnNumber = GsrnNumber.Create("571234567891234568");
            return new AccountingPoint(gsrnNumber, MeteringPointType.Consumption);
        }

        private (AccountingPoint, ConsumerId, EnergySupplierId, Transaction) CreateTestValues()
        {
            return (
                Create(),
                new ConsumerId(Guid.NewGuid()),
                new EnergySupplierId(Guid.NewGuid()),
                new Transaction(Guid.NewGuid().ToString()));
        }
    }
}
