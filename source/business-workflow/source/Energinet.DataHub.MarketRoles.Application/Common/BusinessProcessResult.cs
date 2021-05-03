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
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Application.Common
{
    public class BusinessProcessResult
    {
        public BusinessProcessResult(string transactionId, List<IBusinessRule> businessRules)
        {
            TransactionId = transactionId;
            SetValidationErrors(businessRules);
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(string transactionId, IBusinessRule businessRule)
        {
            if (businessRule == null) throw new ArgumentNullException(nameof(businessRule));

            TransactionId = transactionId;
            SetValidationErrors(new List<IBusinessRule>() { businessRule });
            Success = ValidationErrors.Count == 0;
        }

        public BusinessProcessResult(string transactionId, List<ValidationError> validationErrors)
        {
            TransactionId = transactionId;
            ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
            Success = ValidationErrors.Count == 0;
        }

        private BusinessProcessResult(string transactionId)
        {
            TransactionId = transactionId;
            Success = true;
        }

        public bool Success { get; }

        public string TransactionId { get; }

        public List<ValidationError> ValidationErrors { get; private set; } = new List<ValidationError>();

        public static BusinessProcessResult Ok(string transactionId)
        {
            return new BusinessProcessResult(transactionId);
        }

        private void SetValidationErrors(List<IBusinessRule> rules)
        {
            ValidationErrors = rules
                .Where(r => r.IsBroken)
                .Select(r => new ValidationError(r.Message, r.GetType()))
                .ToList();
        }
    }
}
