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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints
{
    [UnitTest]
    public class MoveInTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider = new SystemDateTimeProviderStub();

        public MoveInTests()
        {
            _systemDateTimeProvider.SetNow(Instant.FromUtc(2020, 1, 1, 0, 0));
        }

        [Fact]
        public void Effectuate_WhenAheadOfEffectiveDate_IsNotPossible()
        {
            var (accountingPoint, consumerId, energySupplierId, processId) = CreateTestValues();
            var moveInDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(1));
            accountingPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, processId);

            Assert.Throws<BusinessProcessException>(() =>
                accountingPoint.EffectuateConsumerMoveIn(processId, _systemDateTimeProvider));
        }

        [Fact]
        public void Effectuate_WhenProcessIdDoesNotExists_IsNotPossible()
        {
            var (accountingPoint, _, _, _) = CreateTestValues();
            var nonExistingProcessId = new ProcessId("NonExisting");

            Assert.Throws<BusinessProcessException>(() =>
                accountingPoint.EffectuateConsumerMoveIn(nonExistingProcessId, _systemDateTimeProvider));
        }

        [Fact]
        public void Accept_WhenPendingMoveInOnSameEffectiveDate_IsNotPossible()
        {
            var meteringPoint = Create();
            var consumerId = new ConsumerId(Guid.NewGuid());
            var energySupplierId = new EnergySupplierId(Guid.NewGuid());
            var moveInDate = _systemDateTimeProvider.Now();
            var processId = new ProcessId(Guid.NewGuid().ToString());

            meteringPoint.AcceptConsumerMoveIn(consumerId, energySupplierId, moveInDate, processId);

            var result = meteringPoint.ConsumerMoveInAcceptable(consumerId, energySupplierId, moveInDate, processId);

            Assert.Contains(result.Errors, x => x.Rule == typeof(MoveInRegisteredOnSameDateIsNotAllowedRule));
        }

        private AccountingPoint Create()
        {
            var gsrnNumber = GsrnNumber.Create("571234567891234568");
            return new AccountingPoint(gsrnNumber, MeteringPointType.Consumption);
        }

        private (AccountingPoint, ConsumerId, EnergySupplierId, ProcessId) CreateTestValues()
        {
            return (
                Create(),
                new ConsumerId(Guid.NewGuid()),
                new EnergySupplierId(Guid.NewGuid()),
                new ProcessId(Guid.NewGuid().ToString()));
        }
    }
}
