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
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges.ValidationRules.Rules;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using FluentAssertions;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges.ValidationRules
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class StartDateVr209ValidationRuleTests
    {
        [Theory]
        [InlineAutoDomainData(32)]
        [InlineAutoDomainData(365)]
        [InlineAutoDomainData(1095)]
        public void Validate_WhenCalledWithValidDate_ShouldReturnTrue(
            int changeOfChargesStartDay,
            StartDateVr209ValidationRule sut)
        {
            var ruleConfigurations = GetValidationRuleConfigurationCollection();

            var messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord
                {
                    ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)),
                },
            };

            var result = sut.Validate(messageWithLowStartDate, ruleConfigurations);

            result.ValidatedSuccessfully.Should().BeTrue();
        }

        [Theory]
        [InlineAutoDomainData(-10)]
        [InlineAutoDomainData(15)]
        [InlineAutoDomainData(31)]
        public void Validate_WhenCalledWithTooEarlyDate_ShouldReturnFalse(int changeOfChargesStartDay,  StartDateVr209ValidationRule sut)
        {
            var ruleConfigurations = GetValidationRuleConfigurationCollection();

            var messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord
                {
                    ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)),
                },
            };

            var result = sut.Validate(messageWithLowStartDate, ruleConfigurations);

            result.Should().NotBeNull();
            result.ValidationError!.Code.Should().Be("VR209");
        }

        [Theory]
        [InlineAutoDomainData(1096)]
        public void Validate_WhenCalledWithTooLateDate_ShouldReturnFalse(int changeOfChargesStartDay, StartDateVr209ValidationRule sut)
        {
            var ruleConfigurations = GetValidationRuleConfigurationCollection();

            var messageWithLowStartDate = new ChangeOfChargesMessage
            {
                MktActivityRecord = new MktActivityRecord
                {
                    ValidityStartDate = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(changeOfChargesStartDay)),
                },
            };

            var result = sut.Validate(messageWithLowStartDate, ruleConfigurations);

            result.Should().NotBeNull();
            result.ValidationError!.Code.Should().Be("VR209");
        }

        private IEnumerable<ValidationRuleConfiguration> GetValidationRuleConfigurationCollection()
        {
            var rules = new List<ValidationRuleConfiguration>
            {
                new ValidationRuleConfiguration(ValidationRuleNames.StartOfValidIntervalFromNowInDays, "31"),
                new ValidationRuleConfiguration(ValidationRuleNames.EndOfValidIntervalFromNowInDays, "1095"),
            };

            return rules;
        }
    }
}
