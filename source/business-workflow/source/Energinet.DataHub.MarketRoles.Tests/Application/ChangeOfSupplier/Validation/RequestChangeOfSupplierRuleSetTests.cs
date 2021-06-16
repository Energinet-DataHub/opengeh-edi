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
using System.Text;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Validation;
using Energinet.DataHub.MarketRoles.Application.Common.Validation;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using NodaTime;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketRoles.Tests.Application.ChangeOfSupplier.Validation
{
    [UnitTest]
    public class RequestChangeOfSupplierRuleSetTests
    {
        [Fact]
        public void Validate_WhenGlnNumberIsEmpty_IsFailure()
        {
            var businessRequest = CreateRequest(string.Empty, string.Empty, string.Empty);

            var errors = GetValidationErrors(businessRequest);

            Assert.Contains(errors, error => error is GlnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenGlnNumberIsNotEmpty_IsSuccess()
        {
            var businessRequest = CreateRequest(string.Empty, SampleData.GlnNumber, string.Empty);

            var errors = GetValidationErrors(businessRequest);

            Assert.DoesNotContain(errors, error => error is GlnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenGsrnNumberIsEmpty_IsFailure()
        {
            var businessRequest = CreateRequest(string.Empty, string.Empty, string.Empty);

            var errors = GetValidationErrors(businessRequest);

            Assert.Contains(errors, error => error is GsrnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenGsrnNumberIsNotFormattedCorrectly_IsFailure()
        {
            var businessRequest = CreateRequest(string.Empty, string.Empty, "Not_Valid_Gsrn_Number");

            var errors = GetValidationErrors(businessRequest);

            Assert.Contains(errors, error => error is GsrnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenGsrnNumberIsValid_IsSuccess()
        {
            var businessRequest = CreateRequest(string.Empty, string.Empty, SampleData.GsrnNumber);

            var errors = GetValidationErrors(businessRequest);

            Assert.DoesNotContain(errors, error => error is GsrnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenTransactionIdIsEmpty_IsFailure()
        {
            var businessRequest = CreateRequest(string.Empty, SampleData.GlnNumber, SampleData.GsrnNumber);

            var errors = GetValidationErrors(businessRequest);

            Assert.Contains(errors, error => error is TransactionMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenTransactionIdIsNotEmpty_IsSuccess()
        {
            var businessRequest = CreateRequest(SampleData.TranactionId, SampleData.GlnNumber, SampleData.GsrnNumber);

            var errors = GetValidationErrors(businessRequest);

            Assert.DoesNotContain(errors, error => error is GsrnNumberMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenStartOfSupplyDateNotValid_IsFailure()
        {
            var businessRequest = CreateRequest(SampleData.TranactionId, SampleData.GlnNumber, SampleData.GsrnNumber, default);

            var errors = GetValidationErrors(businessRequest);

            Assert.Contains(errors, error => error is StartOfSupplyMustBeValidRuleError);
        }

        [Fact]
        public void Validate_WhenStartOfSupplyDateValid_IsSuccess()
        {
            var businessRequest = CreateRequest(SampleData.TranactionId, SampleData.GlnNumber, SampleData.GsrnNumber);

            var errors = GetValidationErrors(businessRequest);

            Assert.DoesNotContain(errors, error => error is StartOfSupplyMustBeValidRuleError);
        }

        private RequestChangeOfSupplier CreateRequest(string transaction = "", string glnNumber = "", string gsrnNumber = "")
        {
            return new RequestChangeOfSupplier(
                transaction,
                glnNumber,
                "FakeCVROrCPR",
                gsrnNumber,
                SystemClock.Instance.GetCurrentInstant());
        }

        private RequestChangeOfSupplier CreateRequest(string transaction = "", string glnNumber = "", string gsrnNumber = "", Instant startDate = default)
        {
            return new RequestChangeOfSupplier(
                transaction,
                glnNumber,
                "FakeCVROrCPR",
                gsrnNumber,
                startDate);
        }

        private List<ValidationError> GetValidationErrors(RequestChangeOfSupplier request)
        {
            var ruleSet = new RequestChangeOfSupplierRuleSet();
            var validationResult = ruleSet.Validate(request);
            return validationResult.Errors
                .Select(error => error.CustomState as ValidationError)
                .ToList();
        }
    }
}
