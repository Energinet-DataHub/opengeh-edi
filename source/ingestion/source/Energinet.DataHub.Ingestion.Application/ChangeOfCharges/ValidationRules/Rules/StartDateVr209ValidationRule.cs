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
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using NodaTime;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules.Rules
{
    public class StartDateVr209ValidationRule : IValidationRule
    {
        public ValidationStatus Validate(ChangeOfChargesMessage changeOfChargesMessage, IEnumerable<ValidationRuleConfiguration> ruleConfigurations)
        {
            var startOfValidIntervalFromNowInDays = ruleConfigurations
                .GetSingleRule(ValidationRuleNames.StartOfValidIntervalFromNowInDays).GetValueAsInteger();
            var endOfValidIntervalFromNowInDays = ruleConfigurations
                .GetSingleRule(ValidationRuleNames.EndOfValidIntervalFromNowInDays).GetValueAsInteger();

            var startOfValidInterval = SystemClock.Instance.GetCurrentInstant()
                .Plus(Duration.FromDays(startOfValidIntervalFromNowInDays));
            var endOfValidInterval = SystemClock.Instance.GetCurrentInstant()
                .Plus(Duration.FromDays(endOfValidIntervalFromNowInDays));
            var startDate = changeOfChargesMessage.MktActivityRecord?.ValidityStartDate;
            var success = startDate >= startOfValidInterval && startDate <= endOfValidInterval;

            return success
                ? new ValidationStatus(success, null)
                : new ValidationStatus(false, new ValidationError("VR209", "Time limits not followed"));
        }
    }
}
