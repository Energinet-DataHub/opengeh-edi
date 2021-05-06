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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints.ChangeOfSupplier
{
    [UnitTest]
    public class CancellationTests
    {
        private SystemDateTimeProviderStub _systemDateTimeProvider;

        public CancellationTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void Cancel_WhenProcessIsPending_Success()
        {
            var (meteringPoint, _) = CreateWithActiveMoveIn();
            var transaction = CreateTransaction();
            meteringPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), _systemDateTimeProvider.Now().Plus(Duration.FromDays(5)), transaction, _systemDateTimeProvider);

            meteringPoint.CancelChangeOfSupplier(transaction);

            Assert.Contains(meteringPoint.DomainEvents !, e => e is ChangeOfSupplierCancelled);
        }

        [Fact]
        public void Cancel_WhenIsNotPending_IsNotPossible()
        {
            var (meteringPoint, _) = CreateWithActiveMoveIn();
            var transaction = CreateTransaction();
            var supplyStartDate = _systemDateTimeProvider.Now();
            meteringPoint.AcceptChangeOfSupplier(CreateEnergySupplierId(), supplyStartDate, transaction, _systemDateTimeProvider);
            meteringPoint.EffectuateChangeOfSupplier(transaction, _systemDateTimeProvider);

            Assert.Throws<BusinessProcessException>(() => meteringPoint.CancelChangeOfSupplier(transaction));
        }

        private (AccountingPoint, Transaction) CreateWithActiveMoveIn()
        {
            var accountingPoint = new AccountingPoint(GsrnNumber.Create("571234567891234568"), MeteringPointType.Consumption);
            var transaction = CreateTransaction();
            accountingPoint.AcceptConsumerMoveIn(CreateConsumerId(), CreateEnergySupplierId(), _systemDateTimeProvider.Now().Minus(Duration.FromDays(365)), transaction);
            accountingPoint.EffectuateConsumerMoveIn(transaction, _systemDateTimeProvider);
            return (accountingPoint, transaction);
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
