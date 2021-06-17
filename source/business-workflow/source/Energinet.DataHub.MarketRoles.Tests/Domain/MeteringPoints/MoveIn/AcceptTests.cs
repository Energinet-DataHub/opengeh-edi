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
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Xunit;

namespace Energinet.DataHub.MarketRoles.Tests.Domain.MeteringPoints.MoveIn
{
    public class AcceptTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;
        private readonly AccountingPoint _accountingPoint;
        private ConsumerId _consumerId;
        private EnergySupplierId _energySupplierId;
        private Transaction _transaction;

        public AcceptTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
            _consumerId = ConsumerId.New();
            _energySupplierId = EnergySupplierId.New();
            _transaction = Transaction.Create(Guid.NewGuid().ToString());
            _accountingPoint = Create();
        }

        [Fact]
        public void Should_raise_event_when_consumer_move_in_is_accepted()
        {
            _accountingPoint.AcceptConsumerMoveIn(ConsumerId.New(), new EnergySupplierId(Guid.NewGuid()), _systemDateTimeProvider.Now(), Transaction.Create(Guid.NewGuid().ToString()));

            Assert.Contains(_accountingPoint.DomainEvents, e => e is ConsumerMoveInAccepted);
        }

        [Fact]
        public void Should_return_error_when_a_pending_movein_with_the_same_movein_date_exists()
        {
            var moveInDate = _systemDateTimeProvider.Now();
            _accountingPoint.AcceptConsumerMoveIn(_consumerId, _energySupplierId, moveInDate, _transaction);

            var result = _accountingPoint.ConsumerMoveInAcceptable(_consumerId, _energySupplierId, moveInDate, _transaction);

            Assert.Contains(result.Errors, error => error is MoveInRegisteredOnSameDateIsNotAllowedRuleError);
        }

        private AccountingPoint Create()
        {
            var gsrnNumber = GsrnNumber.Create(SampleData.GsrnNumber);
            return new AccountingPoint(gsrnNumber, MeteringPointType.Consumption);
        }
    }
}
