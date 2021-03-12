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
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Exceptions;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules
{
    public static class ValidationRuleConfigurationExtensions
    {
        public static int GetValueAsInteger(this ValidationRuleConfiguration ruleConfiguration)
        {
            var success = int.TryParse(ruleConfiguration.Value, out var parsedResult);

            if (success) return parsedResult;

            throw new RuleCouldNotBeMappedException($"Could not map {ruleConfiguration.Value} to an integer");
        }

        public static ValidationRuleConfiguration GetSingleRule(this IEnumerable<ValidationRuleConfiguration> validationRuleConfigurations, string key)
        {
            var rule = validationRuleConfigurations.SingleOrDefault(x => x.Key == key);
            if (rule == null)
            {
                throw new RuleNotFoundException($"Could not find the following key: {key}");
            }

            return rule;
        }
    }
}
