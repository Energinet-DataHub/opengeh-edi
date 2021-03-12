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

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Energinet.DataHub.Ingestion.Application.ChangeOfCharges;
using Energinet.DataHub.Ingestion.Domain.ChangeOfCharges;
using FluentAssertions;
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Queues.ValidationReportDispatcher;
using GreenEnergyHub.Queues.ValidationReportDispatcher.Validation;
using GreenEnergyHub.TestHelpers;
using GreenEnergyHub.TestHelpers.Traits;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Energinet.DataHub.Ingestion.Tests.Application.ChangeOfCharges
{
    [Trait(TraitNames.Category, TraitValues.UnitTest)]
    public class ChangeOfChargesCommandHandlerTests
    {
        [Theory]
        [InlineAutoDomainData]
        public async Task AcceptAsync_WhenCalled_ShouldVerifyThatLoggerIsCalledWithSuccesfulMessage(
            [Frozen] Mock<ILogger<ChangeOfChargesCommandHandler>> logger,
            ChangeOfChargesMessage message,
            ChangeOfChargesCommandHandlerTestable sut)
        {
            await sut.CallAcceptAsync(message).ConfigureAwait(false);

            logger.VerifyLoggerWasCalled($"{nameof(ChangeOfChargesMessage)} have parsed validation", LogLevel.Information);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task RejectAsync_WhenCalled_ShouldSendEventToReportQueue(
            [Frozen] Mock<IValidationReportQueueDispatcher> validationReportQueueDispatcher,
            ChangeOfChargesMessage message,
            [NotNull]ChangeOfChargesCommandHandlerTestable sut)
        {
            await sut.CallRejectAsync(message);

            validationReportQueueDispatcher
                .Verify(mock => mock.DispatchAsync(It.IsAny<IHubMessage>()), Times.Once);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenValidationResultsContainsNoErrors_ShouldReturnTrue(
            [Frozen]Mock<IChangeOfChargesDomainValidator> changeOfChargesValidationRules,
            [Frozen] Mock<IChangeOfChargesInputValidator> inputValidator,
            [NotNull]ChangeOfChargesCommandHandlerTestable sut)
        {
            inputValidator.Setup(iv => iv.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(new HubRequestValidationResult("some mRID"));

            changeOfChargesValidationRules.Setup(x =>
                x.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(new HubRequestValidationResult("some mRID"));

            var result = await sut.CallValidateAsync(new ChangeOfChargesMessage()).ConfigureAwait(false);

            result.Should().Be(true);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenValidationResultsContainsErrors_ShouldReturnFalse(
            [Frozen] HubRequestValidationResult hubRequestValidationResult,
            [Frozen]Mock<IChangeOfChargesInputValidator> inputValidator,
            [Frozen]Mock<IChangeOfChargesDomainValidator> changeOfChargesValidationRules,
            [NotNull]ChangeOfChargesCommandHandlerTestable sut)
        {
            inputValidator.Setup(iv => iv.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(new HubRequestValidationResult("some mRID"));

            hubRequestValidationResult.Add(new ValidationError("some error code", "some error message"));
            changeOfChargesValidationRules.Setup(x =>
                x.ValidateAsync(It.IsAny<ChangeOfChargesMessage>())).ReturnsAsync(hubRequestValidationResult);

            var validationResult = await sut.CallValidateAsync(new ChangeOfChargesMessage()).ConfigureAwait(false);

            validationResult.Should().Be(false);
        }

        [Theory]
        [InlineAutoDomainData]
        public async Task ValidateAsync_WhenInputValidationResultsContainsErrors_ShouldReturnFalse(
            [Frozen] HubRequestValidationResult validationResult,
            [Frozen] Mock<IChangeOfChargesInputValidator> inputValidator,
            [NotNull] ChangeOfChargesCommandHandlerTestable sut)
        {
            validationResult.Add(new ValidationError("some error code", "some error message"));
            inputValidator.Setup(iv => iv.ValidateAsync(It.IsAny<ChangeOfChargesMessage>()))
                .ReturnsAsync(validationResult);

            var result = await sut.CallValidateAsync(new ChangeOfChargesMessage()).ConfigureAwait(false);

            result.Should().Be(false);
        }
    }
}
