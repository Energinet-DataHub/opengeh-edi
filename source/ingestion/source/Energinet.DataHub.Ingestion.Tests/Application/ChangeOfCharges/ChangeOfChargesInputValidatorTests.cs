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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Messaging.Validation;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Moq;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesInputValidatorTests
    {
        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenInputValidationResultsContainsOneError_ShouldReturnHubRequestValidationResultWithOneElement(
            [Frozen] Mock<IRuleEngine<ChangeOfChargesMessage>> ruleEngine,
            [NotNull] ChangeOfChargesInputValidator sut)
        {
            var ruleResults = new List<RuleResult>
            {
                new RuleResult("rule01", "msg01"),
            };
            ruleEngine.Setup(re => re.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(RuleResultCollection.From(ruleResults));

            var validationResult = await sut.ValidateAsync(new ChangeOfChargesMessage()).ConfigureAwait(false);

            Assert.Equal(1, validationResult.Errors.Count);
            Assert.False(validationResult.Success);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenInputValidationIsSuccess_ShouldReturnSuccessHubRequestValidationResult(
            [Frozen] Mock<IRuleEngine<ChangeOfChargesMessage>> ruleEngine,
            [NotNull] ChangeOfChargesInputValidator sut)
        {
            ruleEngine.Setup(re => re.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(RuleResultCollection.From(new List<RuleResult>()));

            var validationResult = await sut.ValidateAsync(new ChangeOfChargesMessage()).ConfigureAwait(false);

            Assert.Empty(validationResult.Errors);
            Assert.True(validationResult.Success);
        }
    }
}
