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

using Energinet.DataHub.MarketData.Domain.BusinessProcesses;
using Energinet.DataHub.MarketData.Domain.BusinessProcesses.Exceptions;
using Energinet.DataHub.MarketData.Domain.MeteringPoints.Rules.ChangeEnergySupplier;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketData.Domain.MeteringPoints
{
    internal class BusinessProcess : Entity
    {
        internal BusinessProcess(ProcessId processId, Instant effectiveDate, BusinessProcessType processType)
        {
            ProcessId = processId;
            EffectiveDate = effectiveDate;
            ProcessType = processType;
            Status = BusinessProcessStatus.Pending;
        }

        public ProcessId ProcessId { get; }

        public Instant EffectiveDate { get; }

        public BusinessProcessType ProcessType { get; }

        public BusinessProcessStatus Status { get; private set; }

        public static BusinessProcess CreateFrom(BusinessProcessSnapshot snapshot)
        {
            return new BusinessProcess(
                new ProcessId(snapshot.ProcessId),
                snapshot.EffectiveDate,
                EnumerationType.FromValue<BusinessProcessType>(snapshot.Status));
        }

        public BusinessProcessSnapshot GetSnapshot()
        {
            return new BusinessProcessSnapshot(ProcessId.Value !, EffectiveDate, ProcessType.Id, Status.Id);
        }

        public void EffectuateOrThrow(ISystemDateTimeProvider systemDateTimeProvider)
        {
            EnsureNotTooEarly(systemDateTimeProvider);
            EnsureStatus();
            Status = BusinessProcessStatus.Completed;
        }

        public void CancelOrThrow()
        {
            EnsureStatus();
            Status = BusinessProcessStatus.Cancelled;
        }

        private void EnsureNotTooEarly(ISystemDateTimeProvider systemDateTimeProvider)
        {
            if (EffectiveDate.ToDateTimeUtc().Date > systemDateTimeProvider.Now().ToDateTimeUtc().Date)
            {
                throw new BusinessProcessException(
                    "Pending business processes cannot be effectuated ahead of effective date.");
            }
        }

        private void EnsureStatus()
        {
            if (Status != BusinessProcessStatus.Pending)
            {
                throw new BusinessProcessException(
                    $"Cannot effectuate business process while status is {Status.Name}.");
            }
        }
    }
}
