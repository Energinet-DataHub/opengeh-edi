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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.CalculationResults.Infrastructure.SqlStatements.Mappers.EnergyResult;

public class MeteringPointTypeMapperTests
{
    public static IEnumerable<object[]> ValidDeltaTableMeteringPointType =>
        new Dictionary<string, MeteringPointType>
        {
            { DeltaTableMeteringPointType.Consumption, MeteringPointType.Consumption },
            { DeltaTableMeteringPointType.Production, MeteringPointType.Production },
            { DeltaTableMeteringPointType.Exchange, MeteringPointType.Exchange },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    public static IEnumerable<object[]> InvalidDeltaTableMeteringPointType =>
        new Dictionary<string, Type>
        {
            { DeltaTableMeteringPointType.VeProduction, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.NetProduction, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.SupplyToGrid, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.ConsumptionFromGrid, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.WholesaleServicesInformation, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.OwnProduction, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.NetFromGrid, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.NetToGrid, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.TotalConsumption, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.ElectricalHeating, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.NetConsumption, typeof(ArgumentOutOfRangeException) },
            { DeltaTableMeteringPointType.CapacitySettlement, typeof(ArgumentOutOfRangeException) },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    public static IEnumerable<object[]> ValidMeteringPointType =>
        new Dictionary<MeteringPointType, string>
        {
            { MeteringPointType.Consumption, DeltaTableMeteringPointType.Consumption },
            { MeteringPointType.Production, DeltaTableMeteringPointType.Production },
            { MeteringPointType.Exchange, DeltaTableMeteringPointType.Exchange },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    public static IEnumerable<object[]> InvalidMeteringPointType =>
        new Dictionary<MeteringPointType, Type>
        {
            { MeteringPointType.VeProduction, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.NetProduction, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.SupplyToGrid, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.ConsumptionFromGrid, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.WholesaleServicesInformation, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.OwnProduction, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.NetFromGrid, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.NetToGrid, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.TotalConsumption, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.ElectricalHeating, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.NetConsumption, typeof(ArgumentOutOfRangeException) },
            { MeteringPointType.CapacitySettlement, typeof(ArgumentOutOfRangeException) },
        }.Select(kvp => new object[] { kvp.Key, kvp.Value });

    [Theory]
    [MemberData(nameof(ValidDeltaTableMeteringPointType))]
    public void FromDeltaTableValue_ReturnsExpectedMeteringPointType(string meteringPointDataValue, MeteringPointType expectedMeteringPointType)
    {
        // Act
        var actual = MeteringPointTypeMapper.FromDeltaTableValue(meteringPointDataValue);

        // Assert
        Assert.Equal(expectedMeteringPointType, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidDeltaTableMeteringPointType))]
    public void FromDeltaTableValue_ReturnsExpectedException(string meteringPointDataValue, Type expectedException)
    {
        // Act
        var actual = () => MeteringPointTypeMapper.FromDeltaTableValue(meteringPointDataValue);

        // Assert
        Assert.Throws(expectedException, actual);
    }

    [Fact]
    public void FromDeltaTableValue_WhenInvalidDeltaTableValue_ThrowsArgumentOutOfRangeException()
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
    public void ToDeltaTableValue_ReturnsExpectedDeltaTableValue(MeteringPointType meteringPointType, string expectedMeteringPointDataValue)
    {
        // Act
        var actual = MeteringPointTypeMapper.ToDeltaTableValue(meteringPointType);

        // Assert
        Assert.Equal(expectedMeteringPointDataValue, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidMeteringPointType))]
    public void ToDeltaTableValue_ReturnsExpectedException(MeteringPointType meteringPointType, Type expectedException)
    {
        // Act
        var actual = () => MeteringPointTypeMapper.ToDeltaTableValue(meteringPointType);

        // Assert
        Assert.Throws(expectedException, actual);
    }

    [Fact]
    public void ToDeltaTableValue_WhenMeteringPointTypeIsNull_ReturnsNull()
    {
        // Act
        var actual = MeteringPointTypeMapper.ToDeltaTableValue(null);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void ToDeltaTableValue_WhenInvalidMeteringPointType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidMeteringPointType = MeteringPointType.VeProduction;

        // Act
        var act = () => MeteringPointTypeMapper.ToDeltaTableValue(invalidMeteringPointType);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}
