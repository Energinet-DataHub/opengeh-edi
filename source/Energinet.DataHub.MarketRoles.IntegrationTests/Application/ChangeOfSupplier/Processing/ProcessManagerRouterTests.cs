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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using Microsoft.EntityFrameworkCore;
using Squadron;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application.ChangeOfSupplier.Processing
{
    [IntegrationTest]
    public class ProcessManagerRouterTests : TestHost
    {
        private ProcessManagerRouter _router;
        private BusinessProcessId _businessProcessId;
        private AccountingPoint _accountingPoint;
        private Consumer _consumer;
        private EnergySupplier _energySupplier;
        private Transaction _transaction;

        public ProcessManagerRouterTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _consumer = CreateConsumer();
            _energySupplier = CreateEnergySupplier();
            _accountingPoint = CreateAccountingPoint();
            _transaction = CreateTransaction();
            SetConsumerMovedIn(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId);
            RegisterChangeOfSupplier(_accountingPoint, _consumer.ConsumerId, _energySupplier.EnergySupplierId, _transaction);
            MarketRolesContext.SaveChanges();

            _businessProcessId = GetBusinessProcessId(_transaction);
            _router = new ProcessManagerRouter(ProcessManagerRepository, CommandScheduler);
        }

        [Fact]
        public async Task EnergySupplierChangeIsRegistered_WhenStateIsNotStarted_ForwardMasterDataDetailsCommandIsEnqueued()
        {
            await _router.Handle(CreateSupplierChangeRegisteredEvent(), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<ForwardMeteringPointDetails>();

            Assert.NotNull(command);
            Assert.Equal(_businessProcessId.Value, command.BusinessProcessId);
        }

        [Fact]
        public async Task MeteringPointDetailsAreDispatched_WhenStateIsAwaitingMeteringPointDetailsDispatch_ForwardConsumerDetailsCommandIsEnqueued()
        {
            await _router.Handle(CreateSupplierChangeRegisteredEvent(), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<ForwardConsumerDetails>();
            Assert.NotNull(command);
            Assert.Equal(_businessProcessId.Value, command.BusinessProcessId);
        }

        [Fact]
        public async Task ConsumerDetailsAreDispatched_WhenStateIsAwaitingConsumerDetailsDispatch_NotifyCurrentSupplierCommandIsEnqueued()
        {
            await _router.Handle(CreateSupplierChangeRegisteredEvent(), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<NotifyCurrentSupplier>();
            Assert.NotNull(command);
            Assert.Equal(_businessProcessId.Value, command.BusinessProcessId);
        }

        [Fact]
        public async Task CurrentSupplierIsNotified_WhenStateIsAwaitingCurrentSupplierNotification_ChangeSupplierCommandIsScheduled()
        {
            await _router.Handle(CreateSupplierChangeRegisteredEvent(), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new CurrentSupplierNotified(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var command = await GetEnqueuedCommandAsync<ChangeSupplier>();
            Assert.NotNull(command);
            Assert.Equal(_accountingPoint.Id.Value, command.AccountingPointId);
        }

        [Fact]
        public async Task SupplierIsChanged_WhenStateIsAwaitingSupplierChange_ProcessIsCompleted()
        {
            await _router.Handle(CreateSupplierChangeRegisteredEvent(), CancellationToken.None);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new MeteringPointDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new ConsumerDetailsDispatched(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(new CurrentSupplierNotified(_accountingPoint.Id, _businessProcessId, Transaction), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            await _router.Handle(CreateEnergySupplierChangedEvent(), CancellationToken.None).ConfigureAwait(false);
            await UnitOfWork.CommitAsync();

            var processManager = await ProcessManagerRepository.GetAsync<ChangeOfSupplierProcessManager>(_businessProcessId);
            Assert.True(processManager.IsCompleted());
        }

        private EnergySupplierChangeRegistered CreateSupplierChangeRegisteredEvent()
        {
            return new EnergySupplierChangeRegistered(
                _accountingPoint.Id,
                _accountingPoint.GsrnNumber,
                _businessProcessId,
                _transaction,
                EffectiveDate);
        }

        private EnergySupplierChanged CreateEnergySupplierChangedEvent()
        {
            return new EnergySupplierChanged(
                _accountingPoint.Id.Value,
                _accountingPoint.GsrnNumber.Value,
                _businessProcessId.Value,
                _transaction.Value,
                _energySupplier.EnergySupplierId.Value,
                EffectiveDate);
        }
    }
}
