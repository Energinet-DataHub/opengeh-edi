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
using Energinet.DataHub.MarketRoles.Application.Common.Commands;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Events;
using MediatR;

namespace Energinet.DataHub.MarketRoles.Application.MoveIn.Processing
{
    public class MoveInProcessManagerRouter
        : INotificationHandler<ConsumerMoveInAccepted>,
            INotificationHandler<ConsumerMovedIn>
    {
        private readonly IProcessManagerRepository _processManagerRepository;
        private readonly ICommandScheduler _commandScheduler;

        public MoveInProcessManagerRouter(IProcessManagerRepository processManagerRepository, ICommandScheduler commandScheduler)
        {
            _processManagerRepository = processManagerRepository ?? throw new ArgumentNullException(nameof(processManagerRepository));
            _commandScheduler = commandScheduler ?? throw new ArgumentNullException(nameof(commandScheduler));
        }

        public async Task Handle(ConsumerMoveInAccepted notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await _processManagerRepository.GetAsync<MoveInProcessManager>(BusinessProcessId.Create(notification.BusinessProcessId)).ConfigureAwait(false);
            if (processManager is null)
            {
                processManager = new MoveInProcessManager();
                _processManagerRepository.Add(processManager);
            }

            processManager.When(notification);
            await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
        }

        public async Task Handle(ConsumerMovedIn notification, CancellationToken cancellationToken)
        {
            if (notification == null) throw new ArgumentNullException(nameof(notification));
            var processManager = await GetProcessManagerAsync(notification.BusinessProcessId).ConfigureAwait(false);
            if (processManager is not null)
            {
                processManager.When(notification);
                await EnqueueCommandsAsync(processManager).ConfigureAwait(false);
            }
        }

        private async Task EnqueueCommandsAsync(ProcessManager processManager)
        {
            var enqueuedCommands = new List<Task>();
            processManager.CommandsToSend.ForEach(command => enqueuedCommands.Add(_commandScheduler.EnqueueAsync(command.Command, command.BusinessProcessId, command.ExecutionDate)));
            await Task.WhenAll(enqueuedCommands).ConfigureAwait(false);
        }

        private Task<MoveInProcessManager?> GetProcessManagerAsync(Guid businessProcessId)
        {
            return _processManagerRepository.GetAsync<MoveInProcessManager>(BusinessProcessId.Create(businessProcessId));
        }
    }
}
