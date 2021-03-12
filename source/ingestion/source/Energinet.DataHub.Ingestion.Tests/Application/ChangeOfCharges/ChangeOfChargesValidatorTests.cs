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
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.Repositories;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules.Rules;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using FluentAssertions;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Microsoft.Extensions.Logging;
using Moq;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesValidatorTests
    {
        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_HappyFlow_ShouldNotAddAnyValidationResults(
            [Frozen] Mock<IEnumerable<IValidationRule>> validationRules,
            [Frozen] Mock<IRuleConfigurationRepository> ruleConfigurationRepository,
            [Frozen] Mock<ILogger<ChangeOfChargesDomainValidator>> logger)
        {
            // Arrange
            SetupValidationRulesCollection(validationRules);

            ruleConfigurationRepository.Setup(x => x.GetRuleConfigurationsAsync())
                .ReturnsAsync(new List<ValidationRuleConfiguration>
                {
                    new ValidationRuleConfiguration(ValidationRuleNames.StartOfValidIntervalFromNowInDays, "30"),
                    new ValidationRuleConfiguration(ValidationRuleNames.EndOfValidIntervalFromNowInDays, "1000"),
                });

            var sut = new ChangeOfChargesDomainValidator(
                validationRules.Object,
                ruleConfigurationRepository.Object,
                logger.Object);

            var changeOfChargesMessage = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord
                {
                    ValidityStartDate = SystemClock.Instance.GetCurrentInstant()
                        .Plus(Duration.FromDays(500)),
                },
            };

            // Act
            var validationResult = await sut.ValidateAsync(changeOfChargesMessage);

            // Assert
            validationResult.Errors.Should().BeEmpty();
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenRuleThrowsRuleNotFoundException_ShouldAddValidationResultAndLog(
            [Frozen] Mock<IEnumerable<IValidationRule>> validationRules,
            [Frozen] Mock<IRuleConfigurationRepository> ruleConfigurationRepository,
            [Frozen] Mock<ILogger<ChangeOfChargesDomainValidator>> logger)
        {
            // Arrange
            SetupValidationRulesCollection(validationRules);

            ruleConfigurationRepository.Setup(x => x.GetRuleConfigurationsAsync())
                .ReturnsAsync(new List<ValidationRuleConfiguration>());
            var sut = new ChangeOfChargesDomainValidator(
                validationRules.Object,
                ruleConfigurationRepository.Object,
                logger.Object);

            // Act
            var validationResult = await sut.ValidateAsync(new ChangeOfChargesMessage());

            // Assert
            validationResult.Errors.Single().Code.Should().Be("VRXYZ");
            logger.VerifyLoggerWasCalled("Rule configuration could not be found", LogLevel.Error);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenRuleConfigurationCannotBeCastCorrectly_ShouldAddValidationResultAndLog(
            [Frozen] Mock<IEnumerable<IValidationRule>> validationRules,
            [Frozen] Mock<IRuleConfigurationRepository> ruleConfigurationRepository,
            [Frozen] Mock<ILogger<ChangeOfChargesDomainValidator>> logger)
        {
            // Arrange
            SetupValidationRulesCollection(validationRules);

            ruleConfigurationRepository.Setup(x => x.GetRuleConfigurationsAsync())
                .ReturnsAsync(new List<ValidationRuleConfiguration>
                {
                    new ValidationRuleConfiguration(ValidationRuleNames.StartOfValidIntervalFromNowInDays, "not valid"),
                    new ValidationRuleConfiguration(ValidationRuleNames.EndOfValidIntervalFromNowInDays, "not valid"),
                });

            var sut = new ChangeOfChargesDomainValidator(
                validationRules.Object,
                ruleConfigurationRepository.Object,
                logger.Object);

            // Act
            var validationResult = await sut.ValidateAsync(new ChangeOfChargesMessage());

            // Assert
            validationResult.Errors.Single().Code.Should().Be("VRXYZ");
            logger.VerifyLoggerWasCalled("Rule value could not be mapped", LogLevel.Error);
        }

        private static void SetupValidationRulesCollection(Mock<IEnumerable<IValidationRule>> validationRules)
        {
            var validationRuleList = new List<IValidationRule> { new StartDateVr209ValidationRule() };
            validationRules
                .Setup(x => x.GetEnumerator())
                .Returns(validationRuleList.GetEnumerator());
        }
    }
}
