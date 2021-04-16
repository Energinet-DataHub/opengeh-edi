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

using Energinet.DataHub.MarketData.Domain.Customers;
using FluentValidation.Validators;
using GreenEnergyHub.Messaging.Validation;

namespace Energinet.DataHub.MarketData.Application.InputValidation.MarketEvaluationPoint
{
    public class ConsumerMustHaveCvrOrCprNumber : PropertyRule<GreenEnergyHub.Messaging.MessageTypes.Common.MarketParticipant>
    {
        protected override string Code => "D17";

        protected override bool IsValid(GreenEnergyHub.Messaging.MessageTypes.Common.MarketParticipant propertyValue, PropertyValidatorContext context)
        {
            return !string.IsNullOrEmpty(propertyValue?.Qualifier)
                && (IsValidCvr(propertyValue) || IsValidCpr(propertyValue));
        }

        private static bool IsValidCvr(GreenEnergyHub.Messaging.MessageTypes.Common.MarketParticipant propertyValue)
        {
            return propertyValue.Qualifier == "VA" && CvrNumber.CheckRules(propertyValue.MRID).Success;
        }

        private static bool IsValidCpr(GreenEnergyHub.Messaging.MessageTypes.Common.MarketParticipant propertyValue)
        {
            return propertyValue.Qualifier == "ARR" && CprNumber.CheckRules(propertyValue.MRID).Success;
        }
    }
}
