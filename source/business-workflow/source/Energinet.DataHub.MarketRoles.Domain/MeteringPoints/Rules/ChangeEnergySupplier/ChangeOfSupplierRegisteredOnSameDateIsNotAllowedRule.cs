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
    public class ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule : IBusinessRule
    {
        private readonly IReadOnlyList<BusinessProcess> _businessProcesses;
        private readonly Instant _supplyStartDate;

        internal ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRule(IReadOnlyList<BusinessProcess> businessProcesses, Instant supplyStartDate)
        {
            _businessProcesses = businessProcesses;
            _supplyStartDate = supplyStartDate;
        }

        public bool IsBroken => ProcessAlreadyRegistered();

        public ValidationError Error => new ChangeOfSupplierRegisteredOnSameDateIsNotAllowedRuleError();

        private bool ProcessAlreadyRegistered()
        {
            return _businessProcesses.Any(process =>
                process.EffectiveDate.ToDateTimeUtc().Date.Equals(_supplyStartDate.ToDateTimeUtc().Date)
                && process.ProcessType == BusinessProcessType.ChangeOfSupplier);
        }
    }
}
