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

using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    public class ChangeOfChargesInputValidator : IChangeOfChargesInputValidator
    {
        private readonly IRuleEngine<ChangeOfChargesMessage> _inputValidationRuleEngine;

        public ChangeOfChargesInputValidator(IRuleEngine<ChangeOfChargesMessage> inputValidationRuleEngine)
        {
            _inputValidationRuleEngine = inputValidationRuleEngine;
        }

        public async Task<HubRequestValidationResult> ValidateAsync(ChangeOfChargesMessage changeOfChargesMessage)
        {
            var result = await _inputValidationRuleEngine.ValidateAsync(changeOfChargesMessage).ConfigureAwait(false);

            var hubRequestValidationResult = new HubRequestValidationResult(changeOfChargesMessage.Transaction.MRID);

            foreach (var error in result)
            {
                hubRequestValidationResult.Add(new ValidationError(error.RuleNumber, error.Message));
            }

            return hubRequestValidationResult;
        }
    }
}
