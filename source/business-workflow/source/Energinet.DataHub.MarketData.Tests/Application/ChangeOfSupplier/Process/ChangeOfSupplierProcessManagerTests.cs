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
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Commands;
using Energinet.DataHub.MarketData.Application.ChangeOfSupplier.Process.Events;
using Energinet.DataHub.MarketData.Application.Common.Process;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.MeteringPoints;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketData.Tests.Domain;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.MarketData.Tests.Application.ChangeOfSupplier.Process
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfSupplierProcessManagerTests
    {
        private readonly SystemDateTimeProviderStub _systemDateTimeProvider;

        public ChangeOfSupplierProcessManagerTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        [Fact]
        public void EnergySupplierChangeIsRegistered_WhenStateIsNotStarted_ConfirmationMessageIsSend()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            var @event = new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate);
            processManager.When(@event);

            var command =
                processManager.CommandsToSend.First(c => c.Command is SendConfirmationMessage).Command as
                    SendConfirmationMessage;

            Assert.NotNull(command);
        }

        [Fact]
        public void ConfirmationMessageIsDispatched_WhenStateIsAwaitingConfirmationMessage_MasterDataDetailsIsSend()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null) processManager.When(new ConfirmationMessageDispatched(processId));

            var command =
                processManager.CommandsToSend.First(c => c.Command is SendMeteringPointDetails).Command as
                    SendMeteringPointDetails;

            Assert.NotNull(command);
        }

        [Fact]
        public void ConfirmationMessageIsDispatched_WhenStateIsNotAwaitingConfirmationMessage_ThrowException()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            if (processId.Value != null)
            {
                var @event = new ConfirmationMessageDispatched(processId);

                Assert.Throws<InvalidProcessManagerStateException>(() => processManager.When(@event));
            }
        }

        [Fact]
        public void MeteringPointDetailsAreDispatched_WhenStateIsAwaitingMeteringPointDetailsDispatch_ConsumerDetailsAreSend()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null)
            {
                processManager.When(new ConfirmationMessageDispatched(processId));
                processManager.When(new MeteringPointDetailsDispatched(processId));
            }

            var command =
                processManager.CommandsToSend.First(c => c.Command is SendConsumerDetails).Command as
                    SendConsumerDetails;

            Assert.NotNull(command);
        }

        [Fact]
        public void MeteringPointDetailsAreDispatched_WhenStateIsNotAwaitingMeteringPointDetailsDispatch_ThrowException()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                if (processId.Value != null) processManager.When(new MeteringPointDetailsDispatched(processId));
            });
        }

        [Fact]
        public void ConsumerDetailsAreDispatched_WhenStateIsAwaitingConsumerDetailsDispatch_GridOperatorIsNotified()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null)
            {
                processManager.When(new ConfirmationMessageDispatched(processId));
                processManager.When(new MeteringPointDetailsDispatched(processId));
                processManager.When(new ConsumerDetailsDispatched(processId));
            }

            var command =
                processManager.CommandsToSend.First(c => c.Command is NotifyGridOperator).Command as
                    NotifyGridOperator;

            Assert.NotNull(command);
        }

        [Fact]
        public void ConsumerDetailsAreDispatched_WhenStateIsNotAwaitingConsumerDetailsDispatch_ThrowException()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                if (processId.Value != null) processManager.When(new ConsumerDetailsDispatched(processId));
            });
        }

        [Fact]
        public void GridOperatorIsNotified_WhenStateIsAwaitingGridOperatorNotification_CurrentSupplierIsNotified()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null)
            {
                processManager.When(new ConfirmationMessageDispatched(processId));
                processManager.When(new MeteringPointDetailsDispatched(processId));
                processManager.When(new ConsumerDetailsDispatched(processId));
                processManager.When(new GridOperatorNotified(processId));
            }

            var command =
                processManager.CommandsToSend.First(c => c.Command is NotifyCurrentSupplier).Command as
                    NotifyCurrentSupplier;

            Assert.NotNull(command);
        }

        [Fact]
        public void GridOperatorIsNotified_WhenStateIsNotAwaitingGridOperatorNotification_ThrowException()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                if (processId.Value != null) processManager.When(new GridOperatorNotified(processId));
            });
        }

        [Fact]
        public void CurrentSupplierIsNotified_WhenStateIsAwaitingCurrentSupplierNotification_ChangeSupplierIsScheduled()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null)
            {
                processManager.When(new ConfirmationMessageDispatched(processId));
                processManager.When(new MeteringPointDetailsDispatched(processId));
                processManager.When(new ConsumerDetailsDispatched(processId));
                processManager.When(new GridOperatorNotified(processId));
                processManager.When(new CurrentSupplierNotified(processId));
            }

            var command =
                processManager.CommandsToSend.First(c => c.Command is ChangeSupplier).Command as
                    ChangeSupplier;

            Assert.NotNull(command);
        }

        [Fact]
        public void CurrentSupplierIsNotified_WhenStateIsNotAwaitingCurrentSupplierNotification_ThrowException()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            Assert.Throws<InvalidProcessManagerStateException>(() =>
            {
                if (processId.Value != null) processManager.When(new CurrentSupplierNotified(processId));
            });
        }

        [Fact]
        public void SupplierIsChanged_WhenStateIsAwaitingSupplierChange_ProcessIsCompleted()
        {
            var processManager = Create();
            var (processId, gsrnNumber, effectiveDate) = CreateTestValues();

            processManager.When(new EnergySupplierChangeRegistered(gsrnNumber, processId, effectiveDate));
            if (processId.Value != null)
            {
                processManager.When(new ConfirmationMessageDispatched(processId));
                processManager.When(new MeteringPointDetailsDispatched(processId));
                processManager.When(new ConsumerDetailsDispatched(processId));
                processManager.When(new GridOperatorNotified(processId));
                processManager.When(new CurrentSupplierNotified(processId));
                processManager.When(new EnergySupplierChanged(gsrnNumber.Value, processId, effectiveDate));
            }

            Assert.True(processManager.IsCompleted());
        }

        private ChangeOfSupplierProcessManager Create()
        {
            return new ChangeOfSupplierProcessManager();
        }

        private (ProcessId, GsrnNumber, Instant) CreateTestValues()
        {
            var processId = new ProcessId(Guid.NewGuid().ToString());
            var gsrnNumber = GsrnNumber.Create("571234567891234568");
            var effectiveDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(60));
            return (processId, gsrnNumber, effectiveDate);
        }
    }
}
