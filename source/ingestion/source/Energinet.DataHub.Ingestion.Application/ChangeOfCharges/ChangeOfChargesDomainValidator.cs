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
using System.Threading.Tasks;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Exceptions;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules.Rules;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.Ingestion.Application.ChangeOfCharges
{
    // ReSharper disable once UnusedType.Global
    public class ChangeOfChargesDomainValidator : IChangeOfChargesDomainValidator
    {
        private readonly IEnumerable<IValidationRule> _validationRules;
        private readonly IRuleConfigurationRepository _ruleConfigurationRepository;
        private readonly ILogger<ChangeOfChargesDomainValidator> _logger;
        private IEnumerable<ValidationRuleConfiguration>? _ruleConfigurations;

        public ChangeOfChargesDomainValidator(
            IEnumerable<IValidationRule> validationRules,
            IRuleConfigurationRepository ruleConfigurationRepository,
            ILogger<ChangeOfChargesDomainValidator> logger)
        {
            _validationRules = validationRules;
            _ruleConfigurationRepository = ruleConfigurationRepository;
            _logger = logger;
        }

        public async Task<HubRequestValidationResult> ValidateAsync(ChangeOfChargesMessage changeOfChargesMessage)
        {
            _ruleConfigurations ??= await _ruleConfigurationRepository.GetRuleConfigurationsAsync();

            var validationResult = new HubRequestValidationResult(changeOfChargesMessage.Transaction.MRID);

            foreach (var validationRule in _validationRules)
            {
                HandleValidationRule(changeOfChargesMessage, validationRule, validationResult);
            }

            return validationResult;
        }

        private void HandleValidationRule(
            ChangeOfChargesMessage changeOfChargesMessage,
            IValidationRule validationRule,
            HubRequestValidationResult validationResult)
        {
            const string unknownServerError = "Unknown server error";
            try
            {
                if (_ruleConfigurations == null)
                {
                    validationResult.Add(new ValidationError("VR900", unknownServerError));
                    _logger.LogError($"{nameof(_ruleConfigurations)} was null");
                    return;
                }

                var ruleValidationResult = validationRule.Validate(changeOfChargesMessage, _ruleConfigurations);

                if (ruleValidationResult.ValidatedSuccessfully is false)
                {
                    validationResult.Add(ruleValidationResult.ValidationError);
                }
            }
            catch (RuleNotFoundException ruleNotFoundException)
            {
                validationResult.Add(new ValidationError("VRXYZ", unknownServerError));
                _logger.LogError(ruleNotFoundException, "Rule configuration could not be found");
            }
            catch (RuleCouldNotBeMappedException ruleCouldNotBeMappedException)
            {
                validationResult.Add(new ValidationError("VRXYZ", unknownServerError));
                _logger.LogError(ruleCouldNotBeMappedException, "Rule value could not be mapped");
            }
        }
    }
}
