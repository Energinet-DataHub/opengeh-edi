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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Edi.UnitTests.Builders;
using Energinet.DataHub.Wholesale.Edi.Validation;
using Energinet.DataHub.Wholesale.Edi.Validation.AggregatedTimeSeriesRequest.Rules;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests.Validators.AggregatedTimeSeriesRequest;

public class SettlementMethodValidatorTest
{
    private static readonly ValidationError _invalidSettlementMethod = new("SettlementMethod kan kun benyttes i kombination med E17 og skal være enten D01 og E02 / SettlementMethod can only be used in combination with E17 and must be either D01 or E02", "D15");

    private readonly SettlementMethodValidationRule _sut = new();

    public static IEnumerable<object[]> FaultedMeteringPointAndSettlementMethodCombinations()
    {
        yield return [MeteringPointType.Production.Name, SettlementMethod.Flex.Name];
        yield return [MeteringPointType.Production.Name, SettlementMethod.NonProfiled.Name];
        yield return [MeteringPointType.Production.Name, "invalid-settlement-method"];
        yield return [MeteringPointType.Exchange.Name, SettlementMethod.Flex.Name];
        yield return [MeteringPointType.Exchange.Name, SettlementMethod.NonProfiled.Name];
        yield return [MeteringPointType.Exchange.Name, "invalid-settlement-method"];
        yield return ["not-consumption-metering-point", SettlementMethod.Flex.Name];
        yield return ["not-consumption-metering-point", SettlementMethod.NonProfiled.Name];
        yield return ["not-consumption-metering-point", "invalid-settlement-method"];
        yield return [string.Empty, SettlementMethod.Flex.Name];
        yield return [string.Empty, SettlementMethod.NonProfiled.Name];
        yield return [string.Empty, "invalid-settlement-method"];
        yield return [null!, SettlementMethod.Flex.Name];
        yield return [null!, SettlementMethod.NonProfiled.Name];
        yield return [null!, "invalid-settlement-method"];
    }

    public static IEnumerable<object[]> GetValidSettlementMethods()
    {
        yield return [SettlementMethod.Flex.Name];
        yield return [SettlementMethod.NonProfiled.Name];
    }

    public static IEnumerable<object[]> GetMeteringPointTypeTestData()
    {
        yield return [MeteringPointType.Production.Name];
        yield return [MeteringPointType.Exchange.Name];
        yield return ["not-consumption"];
    }

    [Theory]
    [MemberData(nameof(GetValidSettlementMethods))]
    public async Task Validate_WhenConsumptionAndSettlementMethodIsValid_ReturnsNoValidationErrorsAsync(string settlementMethod)
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithMeteringPointType(MeteringPointType.Consumption.Name)
            .WithSettlementMethod(settlementMethod)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(GetMeteringPointTypeTestData))]
    public async Task Validate_WhenMeteringPointTypeIsGivenAndSettlementMethodIsNull_ReturnsNoValidationErrorsAsync(string meteringPointType)
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithMeteringPointType(meteringPointType)
            .WithSettlementMethod(null)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WhenConsumptionAndSettlementMethodIsInvalid_ReturnsExpectedValidationErrorAsync()
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithMeteringPointType(MeteringPointType.Consumption.Name)
            .WithSettlementMethod("invalid-settlement-method")
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();

        var error = errors.First();
        error.ErrorCode.Should().Be(_invalidSettlementMethod.ErrorCode);
        error.Message.Should().Be(_invalidSettlementMethod.Message);
    }

    [Theory]
    [MemberData(nameof(FaultedMeteringPointAndSettlementMethodCombinations))]
    public async Task Validate_WhenNotConsumptionAndSettlementMethodIsGiven_ReturnsExpectedValidationErrorAsync(string? meteringPointType, string settlementMethod)
    {
        // Arrange
        var message = AggregatedTimeSeriesRequestBuilder
            .AggregatedTimeSeriesRequest()
            .WithMeteringPointType(meteringPointType)
            .WithSettlementMethod(settlementMethod)
            .Build();

        // Act
        var errors = await _sut.ValidateAsync(message);

        // Assert
        errors.Should().ContainSingle();

        var error = errors.First();
        error.ErrorCode.Should().Be(_invalidSettlementMethod.ErrorCode);
        error.Message.Should().Be(_invalidSettlementMethod.Message);
    }
}
