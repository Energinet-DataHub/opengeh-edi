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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Events;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Domain.BusinessProcesses
{
    public class ProcessCoordinator : AggregateRootBase
    {
        private readonly List<BusinessProcess> _processes = new List<BusinessProcess>();

        public ProcessCoordinator(ProcessCoordinatorId processCoordinatorId)
        {
            ProcessCoordinatorId = processCoordinatorId;
        }

        public ProcessCoordinatorId ProcessCoordinatorId { get; }

        public void Register(BusinessProcess businessProcess)
        {
            ThrowIfRegistered(businessProcess.ProcessId);
            ThrowIfBlockingProcessesRegistered(businessProcess);

            SuspendInterferingProcesses(businessProcess);

            _processes.Add(businessProcess);
            AddDomainEvent(new ProcessRegistered(businessProcess.ProcessId));
        }

        public BusinessProcess GetProcessOrThrow(ProcessId processId)
        {
            var process = GetProcess(processId);
            if (process == null)
            {
                throw new ProcessNotFoundException(processId);
            }

            return process;
        }

        public BusinessProcess? GetProcess(ProcessId processId)
        {
            return _processes.Find(p => p.ProcessId.Equals(processId));
        }

        public bool ProcessExists(ProcessId processId)
        {
            return GetProcess(processId) != null;
        }

        public void Cancel(ProcessId processId)
        {
            var process = GetProcessOrThrow(processId);
            process.Cancel();

            _processes.FindAll(p => p.State == ProcessState.Suspended && p.SuspendedByProcessId!.Equals(processId))
                .ForEach(p => p.Reactivate());
        }

        private void ThrowIfRegistered(ProcessId processId)
        {
            if (ProcessExists(processId))
            {
                throw new DuplicateProcessRegistrationException(processId);
            }
        }

        private void ThrowIfBlockingProcessesRegistered(BusinessProcess businessProcess)
        {
            var blockingProcesses = _processes.FindAll(p =>
                p.EffectiveDate.Equals(businessProcess.EffectiveDate) && p.ShouldBlockProcessOf(businessProcess) && p.IsActive());

            if (blockingProcesses.Count > 0)
            {
                throw new BlockingProcessRegisteredException(blockingProcesses.Select(p => p.ProcessId).ToList());
            }
        }

        private void SuspendInterferingProcesses(BusinessProcess businessProcess)
        {
            _processes
                .FindAll(businessProcess.MustSuspendProcessOf)
                .ForEach(p => p.Suspend(businessProcess.ProcessId));
        }
    }
}
