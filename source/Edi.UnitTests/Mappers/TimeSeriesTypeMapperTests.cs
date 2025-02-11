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
using Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;
using Energinet.DataHub.Wholesale.Edi.Mappers;
using Energinet.DataHub.Wholesale.Edi.Models;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests.Mappers;

public class TimeSeriesTypeMapperTests
{
    public static IEnumerable<object[]> GetCombinationsOf_MeteringPointTypeAndSettlementMethod_WithExpectedTimeSeriesType()
    {
        yield return new object[] { "Consumption", string.Empty,                      TimeSeriesType.TotalConsumption };
        yield return new object[] { "Consumption", null!,                             TimeSeriesType.TotalConsumption };
        yield return new object[] { "Consumption", SettlementMethod.NonProfiled.Name, TimeSeriesType.NonProfiledConsumption };
        yield return new object[] { "Consumption", SettlementMethod.Flex.Name,        TimeSeriesType.FlexConsumption };
        yield return new object[] { "Production",  null!,                             TimeSeriesType.Production };
        yield return new object[] { "Exchange",    null!,                             TimeSeriesType.NetExchangePerGa };
    }

    [Theory]
    [MemberData(nameof(GetCombinationsOf_MeteringPointTypeAndSettlementMethod_WithExpectedTimeSeriesType))]
    public void MapTimeSeriesType_WhenValidMeteringPointTypeAndSettlementMethod_ReturnsExpectedType(
        string meteringPointType,
        string? settlementMethod,
        TimeSeriesType expectedType)
    {
        // Act
        var actualType = TimeSeriesTypeMapper.MapTimeSeriesType(meteringPointType, settlementMethod);

        // Assert
        actualType.Should().Be(expectedType);
    }

    [Fact]
    public void MapTimeSeriesType_WhenInvalidMeteringPointType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidMeteringPointType = "invalid-metering-point-type-value";

        // Act
        var act = () => TimeSeriesTypeMapper.MapTimeSeriesType(invalidMeteringPointType, null);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidMeteringPointType);
    }

    [Fact]
    public void MapTimeSeriesType_WhenValidMeteringPointTypeAndInvalidSettlementMethod_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidSettlementMethod = "invalid-settlement-method-value";

        // Act
        var act = () => TimeSeriesTypeMapper.MapTimeSeriesType("Consumption", invalidSettlementMethod);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidSettlementMethod);
    }
}
