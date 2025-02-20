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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.DeltaTableMappers;

public class ChargeTypeMapperTests
{
    public static TheoryData<string, ChargeType> GetChargeTypeTestData =>
        new()
        {
            { "fee", ChargeType.Fee },
            { "subscription", ChargeType.Subscription },
            { "tariff", ChargeType.Tariff },
        };

    [Theory]
    [MemberData(nameof(GetChargeTypeTestData))]
    public void Given_ChargeType_When_MappingToAndFromDeltaTable_Then_ReturnsExpectedValue(string deltaTableValue, ChargeType chargeType)
    {
        // Act
        var actualType = ChargeTypeMapper.FromDeltaTableValue(deltaTableValue);
        var actualDeltaTableValue = ChargeTypeMapper.ToDeltaTableValue(chargeType);

        // Assert
        actualType.Should().Be(chargeType);
        actualDeltaTableValue.Should().Be(deltaTableValue);
    }

    [Fact]
    public void Given_invalidDatabricksChargeType_When_MappingToChargeType_Then_ThrowsException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => ChargeTypeMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidDeltaTableValue);
    }

    [Fact]
    public void Given_invalidChargeType_When_MappingToDatabricksChargeType_Then_ThrowsException()
    {
        // Arrange
        ChargeType invalidChargeTypeValue = null!;

        // Act
        var act = () => ChargeTypeMapper.ToDeltaTableValue(null!);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidChargeTypeValue);
    }
}
