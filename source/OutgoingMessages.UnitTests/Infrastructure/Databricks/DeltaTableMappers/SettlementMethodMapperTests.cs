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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.DeltaTableMappers;

public class SettlementMethodMapperTests
{
    public static IEnumerable<object[]> ValidSettlementMethod =>
        new Dictionary<string, SettlementMethod>
        {
            { DeltaTableSettlementMethod.NonProfiled, SettlementMethod.NonProfiled },
            { DeltaTableSettlementMethod.Flex, SettlementMethod.Flex },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    [Theory]
    [MemberData(nameof(ValidSettlementMethod))]
    public void FromDeltaTableValue_ReturnsValidSettlementMethod(string? deltaValue, SettlementMethod? expected)
    {
        // Act
        var actual = SettlementMethodMapper.FromDeltaTableValue(deltaValue);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromDeltaTableValue_WhenDeltaValueIsNull_ReturnsNull()
    {
        // Act
        var actual = SettlementMethodMapper.FromDeltaTableValue(null);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void FromDeltaTableValue_WhenInvalidDeltaTableValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => SettlementMethodMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}
