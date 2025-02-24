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

using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

public class MeteringPointTypeMapperTests
{
    public static IEnumerable<object[]> ValidDeltaTableMeteringPointType =>
        new Dictionary<string, MeteringPointType>
        {
            { DeltaTableMeteringPointType.Consumption, MeteringPointType.Consumption },
            { DeltaTableMeteringPointType.Production, MeteringPointType.Production },
            { DeltaTableMeteringPointType.Exchange, MeteringPointType.Exchange },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    public static IEnumerable<object[]> ValidMeteringPointType =>
        new Dictionary<MeteringPointType, string>
        {
            { MeteringPointType.Consumption, DeltaTableMeteringPointType.Consumption },
            { MeteringPointType.Production, DeltaTableMeteringPointType.Production },
            { MeteringPointType.Exchange, DeltaTableMeteringPointType.Exchange },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    public static IEnumerable<object[]> InvalidDeltaTableMeteringPointType()
    {
        var fields = typeof(DeltaTableMeteringPointType).GetFields(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var allDeltaTableMeteringPointTypes = fields.Select(f => f.GetValue(null)!.ToString());
        var validDeltaTableMeteringPointTypes = ValidDeltaTableMeteringPointType
            .Select(valid => valid[0].ToString());
        return allDeltaTableMeteringPointTypes.Except(validDeltaTableMeteringPointTypes)
            .Select(invalid => new object[] { invalid!.ToString(), typeof(ArgumentOutOfRangeException) });
    }

    public static IEnumerable<object[]> InvalidMeteringPointType()
    {
        var allMeteringPointTypes = EnumerationType.GetAll<MeteringPointType>();
        var validMeteringPointTypes = ValidMeteringPointType
            .Select(valid => (MeteringPointType)valid[0]);
        return allMeteringPointTypes.Except(validMeteringPointTypes)
            .Select(invalid => new object[] { invalid, typeof(ArgumentOutOfRangeException) });
    }

    [Theory]
    [MemberData(nameof(ValidDeltaTableMeteringPointType))]
    public void Given_DeltaTableValue_When_IsValid_Then_ReturnsExpectedMeteringPointType(string meteringPointDataValue, MeteringPointType expectedMeteringPointType)
    {
        // Act
        var actual = MeteringPointTypeMapper.FromDeltaTableValue(meteringPointDataValue);

        // Assert
        Assert.Equal(expectedMeteringPointType, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidDeltaTableMeteringPointType))]
    public void Given_DeltaTableValue_When_IsInvalid_Then_ReturnsExpectedException(string meteringPointDataValue, Type expectedException)
    {
        // Act
        var actual = () => MeteringPointTypeMapper.FromDeltaTableValue(meteringPointDataValue);

        // Assert
        Assert.Throws(expectedException, actual);
    }

    [Fact]
    public void Given_DeltaTableValue_When_InvalidDeltaTableValue_Then_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => MeteringPointTypeMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [MemberData(nameof(ValidMeteringPointType))]
    public void Given_MeteringPointType_When_IsValid_Then_ReturnsExpectedDeltaTableValue(MeteringPointType meteringPointType, string expectedMeteringPointDataValue)
    {
        // Act
        var actual = MeteringPointTypeMapper.ToDeltaTableValue(meteringPointType);

        // Assert
        Assert.Equal(expectedMeteringPointDataValue, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidMeteringPointType))]
    public void Given_MeteringPointType_When_IsInvalid_Then_ReturnsExpectedDeltaTableValue(MeteringPointType meteringPointType, Type expectedException)
    {
        // Act
        var actual = () => MeteringPointTypeMapper.ToDeltaTableValue(meteringPointType);

        // Assert
        Assert.Throws(expectedException, actual);
    }

    [Fact]
    public void Given_MeteringPointTypeIsNull_When_MappingToDeltaTableValue_Then_ReturnsNull()
    {
        // Act
        var actual = MeteringPointTypeMapper.ToDeltaTableValue(null);

        // Assert
        Assert.Null(actual);
    }
}
