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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Commands;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Events;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier.Processing
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class ProcessManagerRouterTests : TestHost
    {
        public ProcessManagerRouterTests()
        : base()
        {
            Router = new ProcessManagerRouter(ProcessManagerRepository, CommandScheduler);
            var consumer = CreateConsumer();
            var energySupplier = CreateEnergySupplier();
            var accountingPoint = CreateAccountingPoint();
            SetConsumerMovedIn(accountingPoint, consumer.ConsumerId, energySupplier.EnergySupplierId);
            MarketRolesContext.SaveChanges();

            BusinessProcessId = GetBusinessProcessId();
        }

        protected ProcessManagerRouter Router { get; }

        private BusinessProcessId BusinessProcessId { get; }

        [Fact]
        public async Task EnergySupplierChangeIsRegistered_WhenStateIsNotStarted_ForwardMasterDataDetailsCommandIsEnqueued()
        {
            await Router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.SampleGsrnNumber),  BusinessProcessId, EffectiveDate), CancellationToken.None);
            await MarketRolesContext.SaveChangesAsync();

            var command = await GetEnqueuedCommandAsync<SendMeteringPointDetails>();

            Assert.NotNull(command);
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
                processManager.CommandsToSend.First(c => c.Command is NotifyCurrentSupplier).Command as
                    NotifyCurrentSupplier;

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
                processManager.When(new CurrentSupplierNotified(processId));
                processManager.When(new EnergySupplierChanged(gsrnNumber.Value, processId, effectiveDate));
            }

            Assert.True(processManager.IsCompleted());
        }

        private ChangeOfSupplierProcessManager Create()
        {
            return new ChangeOfSupplierProcessManager();
        }

        private (BusinessProcessId, GsrnNumber, Instant) CreateTestValues()
        {
            var processId = new BusinessProcessId(Guid.NewGuid());
            var gsrnNumber = GsrnNumber.Create("571234567891234568");
            var effectiveDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(60));
            return (processId, gsrnNumber, effectiveDate);
        }

        private async Task<TCommand> GetEnqueuedCommandAsync<TCommand>()
        {
            var type = typeof(TCommand).FullName;
            var queuedCommand = await MarketRolesContext.QueuedInternalCommands
                .SingleOrDefaultAsync(command =>
                command.Type.Equals(type) && command.BusinessProcessId.Equals(BusinessProcessId.Value));

            return Serializer.Deserialize<TCommand>(queuedCommand.Data);
        }
    }
}
