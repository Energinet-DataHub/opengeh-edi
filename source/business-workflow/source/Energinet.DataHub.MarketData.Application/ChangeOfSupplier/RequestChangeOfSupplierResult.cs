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
using Energinet.DataHub.MarketData.Domain.SeedWork;

namespace Energinet.DataHub.MarketData.Application.ChangeOfSupplier
{
    public class RequestChangeOfSupplierResult
    {
        private RequestChangeOfSupplierResult(bool success, List<string>? inputValidationErrors, List<ValidationError>? businessRuleValidationErrors)
        {
            Succeeded = success;
            if (inputValidationErrors != null) InputValidationErrors = inputValidationErrors;
            if (businessRuleValidationErrors != null) BusinessRuleValidationErrors = businessRuleValidationErrors;
        }

        private RequestChangeOfSupplierResult()
        {
            Succeeded = true;
        }

        public bool Succeeded { get; private set; }

        public List<string> InputValidationErrors { get; } = new List<string>();

        public List<ValidationError> BusinessRuleValidationErrors { get; } = new List<ValidationError>();

        public static RequestChangeOfSupplierResult Success()
        {
            return new RequestChangeOfSupplierResult();
        }

        public static RequestChangeOfSupplierResult Reject(List<string> inputValidationErrors)
        {
            return new RequestChangeOfSupplierResult(false, inputValidationErrors, null);
        }

        public static RequestChangeOfSupplierResult Reject(List<ValidationError> businessRuleValidationErrors)
        {
            return new RequestChangeOfSupplierResult(false, null, businessRuleValidationErrors);
        }

        public static RequestChangeOfSupplierResult Reject(string reason)
        {
            return new RequestChangeOfSupplierResult(false, new List<string>() { reason }, null);
        }
    }
}
