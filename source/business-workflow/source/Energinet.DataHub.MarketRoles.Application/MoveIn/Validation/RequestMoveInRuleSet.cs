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

using System.Data;
using Energinet.DataHub.MarketRoles.Application.Common.Validation;
using Energinet.DataHub.MarketRoles.Application.Consumers.Validation;
using FluentValidation;

namespace Energinet.DataHub.MarketRoles.Application.MoveIn.Validation
{
    public class RequestMoveInRuleSet : AbstractValidator<RequestMoveIn>
    {
        public RequestMoveInRuleSet()
        {
            RuleFor(request => request.AccountingPointGsrnNumber).SetValidator(new GsrnNumberMustBeValidRule());
            RuleFor(request => request.SocialSecurityNumber)
                .SetValidator(new SocialSecurityNumberMustBeValid())
                .When(x => !string.IsNullOrEmpty(x.SocialSecurityNumber));
            RuleFor(request => request.VATNumber)
                .SetValidator(new VATNumberMustBeValidRule())
                .When(x => !string.IsNullOrEmpty(x.VATNumber));
        }
    }
}
