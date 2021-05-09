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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.ConsumerDetails;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.EndOfSupplyNotification;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing.MeteringPointDetails;
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing
{
    public class ProcessManagerRouter
        : INotificationHandler<EnergySupplierChangeRegistered>,
            INotificationHandler<MeteringPointDetailsDispatched>,
            INotificationHandler<ConsumerDetailsDispatched>,
            INotificationHandler<CurrentSupplierNotified>,
            INotificationHandler<EnergySupplierChanged>
    {
        private readonly IProcessManagerRepository _processManagerRepository;
        private readonly ICommandScheduler _commandScheduler;

        public ProcessManagerRouter(IProcessManagerRepository processManagerRepository, ICommandScheduler commandScheduler)
        {
            _processManagerRepository = processManagerRepository;
            _commandScheduler = commandScheduler;
        }

        public async Task Handle(EnergySupplierChangeRegistered notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        public async Task Handle(CurrentSupplierNotified notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        public async Task Handle(EnergySupplierChanged notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        public async Task Handle(MeteringPointDetailsDispatched notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        public async Task Handle(ConsumerDetailsDispatched notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        private async Task<ChangeOfSupplierProcessManager> GetProcessManagerAsync(BusinessProcessId businessProcessId)
        {
            var processManager = await _processManagerRepository.GetAsync<ChangeOfSupplierProcessManager>(businessProcessId).ConfigureAwait(false);
            if (processManager is null)
            {
                processManager = new ChangeOfSupplierProcessManager();
                _processManagerRepository.Add(processManager);
            }

            return processManager;
        }

        private async Task EnqueueCommandsAsync(ProcessManager processManager)
        {
            var enqueuedCommands = new List<Task>();
            processManager.CommandsToSend.ForEach(command => enqueuedCommands.Add(_commandScheduler.EnqueueAsync(command.Command, command.BusinessProcessId, command.ExecutionDate)));
            await Task.WhenAll(enqueuedCommands).ConfigureAwait(false);
        }
    }
}
