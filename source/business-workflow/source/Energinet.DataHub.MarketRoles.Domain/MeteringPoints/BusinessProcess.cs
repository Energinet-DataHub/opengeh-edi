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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints
{
    public class BusinessProcess : Entity
    {
        internal BusinessProcess(BusinessProcessId businessProcessId, Transaction transaction, Instant effectiveDate, BusinessProcessType processType)
        {
            BusinessProcessId = businessProcessId ?? throw new ArgumentNullException(nameof(businessProcessId));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            EffectiveDate = effectiveDate;
            ProcessType = processType ?? throw new ArgumentNullException(nameof(processType));
            Status = BusinessProcessStatus.Pending;
        }

#pragma warning disable 8618
        private BusinessProcess()
#pragma warning restore 8618
        {
            // For EF core
        }

        public BusinessProcessId BusinessProcessId { get; }

        public Transaction Transaction { get; }

        public Instant EffectiveDate { get; }

        public BusinessProcessType ProcessType { get; }

        public BusinessProcessStatus Status { get; private set; }

        public void Effectuate(ISystemDateTimeProvider systemDateTimeProvider)
        {
            EnsureNotTooEarly(systemDateTimeProvider);
            EnsureStatus();
            Status = BusinessProcessStatus.Completed;
        }

        public void Cancel()
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
