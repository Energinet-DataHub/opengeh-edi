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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
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
            var transaction = CreateTransaction();
            accountingPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, transaction, _systemDateTimeProvider);

            Assert.Throws<BusinessProcessException>(() => accountingPoint.EffectuateChangeOfSupplier(transaction, _systemDateTimeProvider));
        }

        [Fact]
        public void Effectuate_WhenCurrentDateIsEffectiveDate_IsSuccess()
        {
            var accountingPoint = CreateTestObject();

            var supplyStartDate = _systemDateTimeProvider.Now();
            var transaction = CreateTransaction();
            accountingPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, transaction, _systemDateTimeProvider);
            accountingPoint.EffectuateChangeOfSupplier(transaction, _systemDateTimeProvider);

            var @event =
                accountingPoint.DomainEvents.FirstOrDefault(e => e is EnergySupplierChanged) as EnergySupplierChanged;

            Assert.NotNull(@event);
        }

        private AccountingPoint CreateTestObject()
        {
            var accountingPoint = new AccountingPoint(GsrnNumber.Create("571234567891234568"), MeteringPointType.Consumption);
            var transaction = CreateTransaction();
            accountingPoint.AcceptConsumerMoveIn(CreateConsumerId(), CreateEnergySupplierId(), _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)), transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, _systemDateTimeProvider);
            return accountingPoint;
        }

        private Transaction CreateTransaction()
        {
            return new Transaction(Guid.NewGuid().ToString());
        }

        private EnergySupplierId CreateEnergySupplierId()
        {
            return new EnergySupplierId(Guid.NewGuid());
        }

        private ConsumerId CreateConsumerId()
        {
            return new ConsumerId(Guid.NewGuid());
        }
    }
}
