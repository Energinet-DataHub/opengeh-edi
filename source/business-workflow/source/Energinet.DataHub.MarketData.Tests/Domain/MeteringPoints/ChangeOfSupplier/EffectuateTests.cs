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
using System.Linq;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Domain.MeteringPoints
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class EffectuateTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public EffectuateTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Effectuate_WhenBeforeOfEffectiveDate_IsNotPossible()
        {
            var accountingPoint = CreateTestObject();

            var supplyStartDate = _systemDateTimeProvider.Now().Plus(Duration.FromDays(5));
            var processId = CreateProcessId();
            accountingPoint.AcceptChangeOfSupplier(new EnergySupplierId("NewEnergySupplierId"), supplyStartDate, processId, _systemDateTimeProvider);

            Assert.Throws<BusinessProcessException>(() => accountingPoint.EffectuateChangeOfSupplier(processId, _systemDateTimeProvider));
        }

        [Fact]
        public void Effectuate_WhenCurrentDateIsEffectiveDate_IsSuccess()
        {
            var accountingPoint = CreateTestObject();

            var supplyStartDate = _systemDateTimeProvider.Now();
            var processId = CreateProcessId();
            accountingPoint.AcceptChangeOfSupplier(new EnergySupplierId("NewEnergySupplierId"), supplyStartDate, processId, _systemDateTimeProvider);
            accountingPoint.EffectuateChangeOfSupplier(processId, _systemDateTimeProvider);

            var @event =
                accountingPoint.DomainEvents.FirstOrDefault(e => e is EnergySupplierChanged) as EnergySupplierChanged;

            Assert.NotNull(@event);
        }

        private AccountingPoint CreateTestObject()
        {
            var accountingPoint = new AccountingPoint(GsrnNumber.Create("571234567891234568"), MeteringPointType.Consumption);
            var processId = new ProcessId(Guid.NewGuid().ToString());
            accountingPoint.AcceptConsumerMoveIn(new ConsumerId("FakeConsumerId"), new EnergySupplierId("FakeEnergySupplierId"), _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)), processId);
            accountingPoint.EffectuateConsumerMoveIn(processId, _systemDateTimeProvider);
            return accountingPoint;
        }

        private ProcessId CreateProcessId()
        {
            return new ProcessId(Guid.NewGuid().ToString());
        }
    }
}
