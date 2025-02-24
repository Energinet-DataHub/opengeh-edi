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

using System.Text.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.DeltaTableMappers;

public class QuantityQualityMapperTests
{
    public static TheoryData<string, QuantityQuality> GetQuantityQualityTestData =>
    new()
    {
                { "calculated", QuantityQuality.Calculated },
                { "estimated", QuantityQuality.Estimated },
                { "measured", QuantityQuality.Measured },
                { "missing", QuantityQuality.Missing },
    };

    [Theory]
    [MemberData(nameof(GetQuantityQualityTestData))]
    public void Given_QuantityQuality_When_MappingToAndFromDeltaTable_Then_ReturnsExpectedValue(string deltaValue, QuantityQuality expected)
    {
        // Arrange
        var value = $"[\"{deltaValue}\"]";

        // Act
        var actual = QuantityQualityMapper.FromDeltaTableValues(value);

        // Assert
        actual.Should().Contain(expected);
    }

    [Fact]
    public void Given_InvalidDatabricksQuantityQuality_When_MappingToQuantityQuality_Then_ThrowsException()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid().ToString();
        var invalidDeltaTableValue = $"[\"{expectedGuid}\"]";

        // Act
        var act = () => QuantityQualityMapper.FromDeltaTableValues(invalidDeltaTableValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(expectedGuid);
    }

    [Fact]
    public void Given_NullDatabricksQuantityQuality_When_MappingToQuantityQuality_Then_ReturnsNull()
    {
        // Arrange
        string? nullDeltaTableValue = null;

        // Assert
        var act = QuantityQualityMapper.TryFromDeltaTableValues(nullDeltaTableValue);

        // Act
        act.Should().BeNull();
    }
}
