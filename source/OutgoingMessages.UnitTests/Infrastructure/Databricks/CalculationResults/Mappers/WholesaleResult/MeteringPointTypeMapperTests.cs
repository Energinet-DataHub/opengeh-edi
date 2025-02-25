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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResults;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.CalculationResults.Mappers.WholesaleResult;

public class MeteringPointTypeMapperTests
{
    public static TheoryData<string, MeteringPointType> MeteringPointTypeData()
    {
        return new TheoryData<string, MeteringPointType>
        {
            { DeltaTableMeteringPointType.Consumption, MeteringPointType.Consumption },
            { DeltaTableMeteringPointType.Production, MeteringPointType.Production },
            { DeltaTableMeteringPointType.Exchange, MeteringPointType.Exchange },
            { DeltaTableMeteringPointType.VeProduction, MeteringPointType.VeProduction },
            { DeltaTableMeteringPointType.NetProduction, MeteringPointType.NetProduction },
            { DeltaTableMeteringPointType.SupplyToGrid, MeteringPointType.SupplyToGrid },
            { DeltaTableMeteringPointType.ConsumptionFromGrid, MeteringPointType.ConsumptionFromGrid },
            { DeltaTableMeteringPointType.WholesaleServicesInformation, MeteringPointType.WholesaleServicesInformation },
            { DeltaTableMeteringPointType.OwnProduction, MeteringPointType.OwnProduction },
            { DeltaTableMeteringPointType.NetFromGrid, MeteringPointType.NetFromGrid },
            { DeltaTableMeteringPointType.NetToGrid, MeteringPointType.NetToGrid },
            { DeltaTableMeteringPointType.TotalConsumption, MeteringPointType.TotalConsumption },
            { DeltaTableMeteringPointType.ElectricalHeating, MeteringPointType.ElectricalHeating },
            { DeltaTableMeteringPointType.NetConsumption, MeteringPointType.NetConsumption },
            { DeltaTableMeteringPointType.CapacitySettlement, MeteringPointType.CapacitySettlement },
        };
    }

    [Theory]
    [MemberData(nameof(MeteringPointTypeData))]
    public void Given_ValidDeltaTableValue_When_MappedToMeteringPointType_Then_ReturnsExpected(string? deltaValue, MeteringPointType? expected)
    {
        // Act
        var actual = MeteringPointTypeMapper.FromDeltaTableValue(deltaValue);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void Given_DeltaTableValueIsNull_When_MappedToMeteringPointType_Then_ReturnsNull()
    {
        // Act
        var actual = MeteringPointTypeMapper.FromDeltaTableValue(null);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void Given_InvalidDeltaTableValue_When_MappedToMeteringPointType_Then_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => MeteringPointTypeMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidDeltaTableValue);
    }
}
