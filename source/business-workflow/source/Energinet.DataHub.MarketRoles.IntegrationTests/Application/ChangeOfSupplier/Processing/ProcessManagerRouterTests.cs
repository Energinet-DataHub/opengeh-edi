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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Commands;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.Events;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier.Processing
{
    [Collection("IntegrationTest")]
    [IntegrationTest]
    public class ProcessManagerRouterTests : TestHost
    {
        private ProcessManagerRouter _router;
        private BusinessProcessId _businessProcessId;

        public ProcessManagerRouterTests()
        : base()
        {
            var consumer = CreateConsumer();
            var energySupplier = CreateEnergySupplier();
            var accountingPoint = CreateAccountingPoint();
            SetConsumerMovedIn(accountingPoint, consumer.ConsumerId, energySupplier.EnergySupplierId);
            MarketRolesContext.SaveChanges();

            _businessProcessId = GetBusinessProcessId();
            _router = new ProcessManagerRouter(ProcessManagerRepository, CommandScheduler);
        }

        [Fact]
        public async Task EnergySupplierChangeIsRegistered_WhenStateIsNotStarted_ForwardMasterDataDetailsCommandIsEnqueued()
        {
            await _router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.GsrnNumber),  _businessProcessId, EffectiveDate), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<SendMeteringPointDetails>();

            Assert.NotNull(command);
            Assert.Equal(_businessProcessId, command.BusinessProcessId);
        }

        [Fact]
        public async Task MeteringPointDetailsAreDispatched_WhenStateIsAwaitingMeteringPointDetailsDispatch_ForwardConsumerDetailsCommandIsEnqueued()
        {
            await _router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.GsrnNumber),  _businessProcessId, EffectiveDate), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<SendConsumerDetails>();
            Assert.NotNull(command);
            Assert.Equal(_businessProcessId, command.BusinessProcessId);
        }

        [Fact]
        public async Task ConsumerDetailsAreDispatched_WhenStateIsAwaitingConsumerDetailsDispatch_NotifyCurrentSupplierCommandIsEnqueued()
        {
            await _router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.GsrnNumber),  _businessProcessId, EffectiveDate), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<NotifyCurrentSupplier>();
            Assert.NotNull(command);
            Assert.Equal(_businessProcessId, command.BusinessProcessId);
        }

        [Fact]
        public async Task CurrentSupplierIsNotified_WhenStateIsAwaitingCurrentSupplierNotification_ChangeSupplierCommandIsScheduled()
        {
            await _router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.GsrnNumber),  _businessProcessId, EffectiveDate), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new CurrentSupplierNotified(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<ChangeSupplier>();
            Assert.NotNull(command);
            Assert.Equal(_businessProcessId, command.BusinessProcessId);
        }

        [Fact]
        public async Task SupplierIsChanged_WhenStateIsAwaitingSupplierChange_ProcessIsCompleted()
        {
            await _router.Handle(new EnergySupplierChangeRegistered(GsrnNumber.Create(SampleData.GsrnNumber),  _businessProcessId, EffectiveDate), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new CurrentSupplierNotified(_businessProcessId), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new EnergySupplierChanged(SampleData.GsrnNumber, _businessProcessId, EffectiveDate), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var processManager = await ProcessManagerRepository.GetAsync<ChangeOfSupplierProcessManager>(_businessProcessId);
            Assert.True(processManager.IsCompleted());
        }

        private async Task<TCommand> GetEnqueuedCommandAsync<TCommand>()
        {
            var type = typeof(TCommand).FullName;
            var queuedCommand = await MarketRolesContext.QueuedInternalCommands
                .SingleOrDefaultAsync(command =>
                command.Type.Equals(type) && command.BusinessProcessId.Equals(_businessProcessId.Value));

            return Serializer.Deserialize<TCommand>(queuedCommand.Data);
        }
    }
}
