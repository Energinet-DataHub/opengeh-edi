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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Tests.Domain;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Application.ChangeOfSupplier.Process
{
    [UnitTest]
    public class ChangeOfSupplierProcessManagerTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;
        private readonly AccountingPointId _accountingPointId;
        private readonly GsrnNumber _gsrnNumber;
        private readonly BusinessProcessId _businessProcessId;
        private readonly Transaction _transaction;
        private readonly EnergySupplierId _energySupplierId;
        private readonly Instant _effectiveDate;

        public ChangeOfSupplierProcessManagerTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
            _accountingPointId = AccountingPointId.New();
            _gsrnNumber = GsrnNumber.Create("571234567891234568");
            _businessProcessId = new BusinessProcessId(Guid.NewGuid());
            _transaction = new Transaction(Guid.NewGuid().ToString());
            _energySupplierId = new EnergySupplierId(Guid.NewGuid());
            _effectiveDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(60));
        }

        [Fact]
        public void EnergySupplierChangeIsRegistered_WhenStateIsNotStarted_MasterDataDetailsIsSend()
        {
            var processManager = Create();

            processManager.When(CreateSupplierChangeRegisteredEvent());

            var command =
                processManager.CommandsToSend.First(c => c.Command is ForwardMeteringPointDetails).Command as
                    ForwardMeteringPointDetails;

            Assert.NotNull(command);
        }

        [Fact]
        public void MeteringPointDetailsAreDispatched_WhenStateIsAwaitingMeteringPointDetailsDispatch_ConsumerDetailsAreSend()
        {
            var processManager = Create();

            processManager.When(CreateSupplierChangeRegisteredEvent());
            processManager.When(new MeteringPointDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            var command =
                processManager.CommandsToSend.First(c => c.Command is ForwardConsumerDetails).Command as
                    ForwardConsumerDetails;

            Assert.NotNull(command);
        }

        [Fact]
        public void MeteringPointDetailsAreDispatched_WhenStateIsNotAwaitingMeteringPointDetailsDispatch_ThrowException()
        {
            var processManager = Create();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                processManager.When(new MeteringPointDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            });
        }

        [Fact]
        public void ConsumerDetailsAreDispatched_WhenStateIsAwaitingConsumerDetailsDispatch_CurrentSupplierIsNotified()
        {
            var processManager = Create();

            processManager.When(CreateSupplierChangeRegisteredEvent());
            processManager.When(new MeteringPointDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new ConsumerDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));

            var command =
                processManager.CommandsToSend.First(c => c.Command is NotifyCurrentSupplier).Command as
                    NotifyCurrentSupplier;

            Assert.NotNull(command);
        }

        [Fact]
        public void ConsumerDetailsAreDispatched_WhenStateIsNotAwaitingConsumerDetailsDispatch_ThrowException()
        {
            var processManager = Create();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                processManager.When(new ConsumerDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            });
        }

        [Fact]
        public void CurrentSupplierIsNotified_WhenStateIsAwaitingCurrentSupplierNotification_ChangeSupplierIsScheduled()
        {
            var processManager = Create();

            processManager.When(CreateSupplierChangeRegisteredEvent());
            processManager.When(new MeteringPointDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new ConsumerDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new CurrentSupplierNotified(_accountingPointId, _businessProcessId, _transaction));

            var command =
                processManager.CommandsToSend.First(c => c.Command is ChangeSupplier).Command as
                    ChangeSupplier;

            Assert.NotNull(command);
        }

        [Fact]
        public void CurrentSupplierIsNotified_WhenStateIsNotAwaitingCurrentSupplierNotification_ThrowException()
        {
            var processManager = Create();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                processManager.When(new CurrentSupplierNotified(_accountingPointId, _businessProcessId, _transaction));
            });
        }

        [Fact]
        public void SupplierIsChanged_WhenStateIsAwaitingSupplierChange_ProcessIsCompleted()
        {
            var processManager = Create();

            processManager.When(CreateSupplierChangeRegisteredEvent());
            processManager.When(new MeteringPointDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new ConsumerDetailsDispatched(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new CurrentSupplierNotified(_accountingPointId, _businessProcessId, _transaction));
            processManager.When(new EnergySupplierChanged(_accountingPointId.Value, _gsrnNumber.Value, _businessProcessId.Value, _transaction.Value, _energySupplierId.Value, Instant.MaxValue));

            Assert.True(processManager.IsCompleted());
        }

        private static ChangeOfSupplierProcessManager Create()
        {
            return new ChangeOfSupplierProcessManager();
        }

        private EnergySupplierChangeRegistered CreateSupplierChangeRegisteredEvent()
        {
            return new EnergySupplierChangeRegistered(
                _accountingPointId,
                _gsrnNumber,
                _businessProcessId,
                _transaction,
                _effectiveDate);
        }
    }
}
