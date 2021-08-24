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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;

namespace Energinet.DataHub.MarketRoles.Domain.MeteringPoints.Rules.ChangeEnergySupplier
{
    public class MoveInRegisteredOnSameDateIsNotAllowedRule : IBusinessRule
    {
        private readonly IReadOnlyList<BusinessProcess> _businessProcesses;
        private readonly Instant _moveInDate;

        internal MoveInRegisteredOnSameDateIsNotAllowedRule(IReadOnlyList<BusinessProcess> businessProcesses, Instant moveInDate)
        {
            _businessProcesses = businessProcesses;
            _moveInDate = moveInDate;
        }

        public bool IsBroken => HasPendingOrCompletedMoveInProcess();

        public ValidationError ValidationError => new MoveInRegisteredOnSameDateIsNotAllowedRuleError(_moveInDate);

        private bool HasPendingOrCompletedMoveInProcess()
        {
            return _businessProcesses.Any(p =>
                p.ProcessType == BusinessProcessType.MoveIn &&
                p.EffectiveDate.ToDateTimeUtc().Date.Equals(_moveInDate.ToDateTimeUtc().Date) &&
                (p.Status == BusinessProcessStatus.Pending ||
                p.Status == BusinessProcessStatus.Completed));
        }
    }
}
