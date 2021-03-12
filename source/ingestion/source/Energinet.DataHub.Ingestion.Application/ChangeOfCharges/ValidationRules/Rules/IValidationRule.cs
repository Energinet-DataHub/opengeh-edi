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

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules.Rules
{
    /// <summary>
    /// Used on each validation rule.
    /// </summary>
    public interface IValidationRule
    {
        /// <summary>
        /// Used to validate validation rules
        /// </summary>
        /// <param name="changeOfChargesMessage">Message to validate</param>
        /// <param name="ruleConfigurations">Configurable rules</param>
        /// <returns>Returns null if parsed else it returns the validation error</returns>
        ValidationStatus Validate(
            ChangeOfChargesMessage changeOfChargesMessage,
            IEnumerable<ValidationRuleConfiguration> ruleConfigurations);
    }
}
