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
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Events;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.BusinessProcesses
{
    public abstract class BusinessProcess : Entity
    {
        public BusinessProcess(ProcessId processId, Instant effectiveDate)
        {
            ProcessId = processId ?? throw new ArgumentNullException(nameof(processId));
            EffectiveDate = effectiveDate;
            State = ProcessState.Registered;
        }

        public ProcessId ProcessId { get; }

        public Instant EffectiveDate { get; }

        public ProcessState State { get; private set; }

        public ProcessId? SuspendedByProcessId { get; private set; }

        internal virtual bool MustSuspendProcessOf(BusinessProcess businessProcess)
        {
            return false;
        }

        internal virtual bool ShouldBlockProcessOf(BusinessProcess businessProcess)
        {
            return false;
        }

        internal void Suspend(ProcessId suspendingProcessId)
        {
            State = ProcessState.Suspended;
            SuspendedByProcessId = suspendingProcessId;
            AddDomainEvent(new ProcessSuspended(ProcessId, suspendingProcessId));
        }

        internal bool IsActive()
        {
            return !(State == ProcessState.Suspended || State == ProcessState.Cancelled);
        }

        internal void Cancel()
        {
            State = ProcessState.Cancelled;
            AddDomainEvent(new ProcessCancelled(ProcessId));
        }

        protected internal virtual void Reactivate()
        {
            if (State != ProcessState.Suspended)
            {
                throw new ProcessReactivationException(ProcessId, State);
            }

            State = ProcessState.Registered;
            AddDomainEvent(new ProcessReactivated(ProcessId));
        }
    }
}
