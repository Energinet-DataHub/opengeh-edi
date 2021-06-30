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
using Energinet.DataHub.MarketRoles.Application.Consumers.Validation;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Application.MoveIn.Validation;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport.Protobuf;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Application.MoveIn.Validation
{
    [UnitTest]
    public class RequestMoveInRuleSetTests
    {
        [Fact]
        public void Validate_When_SocialSecurityNumber_Is_Valid()
        {
            var request = CreateRequest(SampleData.ConsumerSocialSecurityNumber, SampleData.ConsumerVATNumber);

            var errors = GetValidationErrors(request);

            Assert.DoesNotContain(errors, error => error is SocialSecurityNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_That_SocialSecurityNumber_Validation_Is_Not_Run_When_SocialSecurityNumber_Is_Empty()
        {
            var request = CreateRequest(string.Empty, SampleData.ConsumerVATNumber);

            var errors = GetValidationErrors(request);

            Assert.DoesNotContain(errors, error => error is VATNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_Fails_When_SocialSecurityNumber_Has_Invalid_Date()
        {
            var request = CreateRequest("2091202120", SampleData.ConsumerVATNumber);

            var errors = GetValidationErrors(request);

            Assert.Contains(errors, error => error is SocialSecurityNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_Fails_When_SocialSecurityNumber_Is_Wrong_Length()
        {
            var request = CreateRequest("209120", SampleData.ConsumerVATNumber);

            var errors = GetValidationErrors(request);

            Assert.Contains(errors, error => error is SocialSecurityNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_When_VATNumber_Is_Valid()
        {
            var request = CreateRequest(SampleData.ConsumerSocialSecurityNumber, SampleData.ConsumerVATNumber);

            var errors = GetValidationErrors(request);

            Assert.DoesNotContain(errors, error => error is VATNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_That_SocialSecurityNumber_Validation_Is_Not_Run_When_VATNUmber_Is_Empty()
        {
            var request = CreateRequest(SampleData.ConsumerSocialSecurityNumber, string.Empty);

            var errors = GetValidationErrors(request);

            Assert.DoesNotContain(errors, error => error is VATNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_Fails_When_VATNumber_Is_Wrong_Length()
        {
            var request = CreateRequest(SampleData.ConsumerSocialSecurityNumber, "0998768");

            var errors = GetValidationErrors(request);

            Assert.Contains(errors, error => error is VATNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_Fails_When_VATNumber_Is_OutOfBound()
        {
            var request = CreateRequest(SampleData.ConsumerSocialSecurityNumber, "09987680");

            var errors = GetValidationErrors(request);

            Assert.Contains(errors, error => error is VATNumberMustBeValidRuleError);
        }

        private RequestMoveIn CreateRequest(string socialSecurityNumber, string vatNumber)
        {
            return new(
                SampleData.Transaction,
                SampleData.GlnNumber,
                socialSecurityNumber,
                vatNumber,
                SampleData.ConsumerName,
                SampleData.GsrnNumber,
                SystemClock.Instance.GetCurrentInstant());
        }

        private List<ValidationError> GetValidationErrors(RequestMoveIn request)
        {
            var ruleSet = new RequestMoveInRuleSet();
            var validationResult = ruleSet.Validate(request);
            return validationResult.Errors
                .Select(error => error.CustomState as ValidationError)
                .ToList();
        }
    }
}
