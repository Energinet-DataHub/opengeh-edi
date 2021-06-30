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
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace Energinet.DataHub.MarketRoles.Application.Common
{
    public class BusinessProcessResult
    {
        public BusinessProcessResult(string transactionId, IEnumerable<IBusinessRule> businessRules)
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

        public BusinessProcessResult(string transactionId, ReadOnlyCollection<ValidationError> validationErrors)
        {
            TransactionId = transactionId;
            ValidationErrors = validationErrors ?? throw new ArgumentNullException(nameof(validationErrors));
            Success = ValidationErrors.Count == 0;
        }

        private BusinessProcessResult(string transactionId, bool success)
        {
            TransactionId = transactionId;
            Success = success;
        }

        public bool Success { get; }

        public string TransactionId { get; }

        public IReadOnlyCollection<ValidationError> ValidationErrors { get; private set; } = new List<ValidationError>();

        public static BusinessProcessResult Ok(string transactionId)
        {
            return new BusinessProcessResult(transactionId, true);
        }

        public static BusinessProcessResult Fail(string transactionId)
        {
            return new BusinessProcessResult(transactionId, false);
        }

        private void SetValidationErrors(IEnumerable<IBusinessRule> rules)
        {
            ValidationErrors = rules
                .Where(r => r.IsBroken)
                .Select(r => r.ValidationError)
                .ToList();
        }
    }
}
