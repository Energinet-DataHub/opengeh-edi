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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;

public class AggregationLevelMapperTests
{
    public static TheoryData<MeteringPointType?, SettlementMethod?, string?, string?, AggregationLevel> AggregationLevelData()
    {
        return new TheoryData<MeteringPointType?, SettlementMethod?, string?, string?, AggregationLevel>
        {
            { MeteringPointType.Exchange, null, null, null, AggregationLevel.GridArea },
            { MeteringPointType.Consumption, null, null, null, AggregationLevel.GridArea },
            { MeteringPointType.Production, null, "energySupplierGln", null, AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea },
            { MeteringPointType.Production, null, "energySupplierGln", "balanceResponsiblePartyGln", AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea },
            { MeteringPointType.Production, null, null, "balanceResponsiblePartyGln", AggregationLevel.BalanceResponsibleAndGridArea },
            { null, null, null, null, AggregationLevel.GridArea },
            { null, null, "energySupplierGln", null, AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea },
            { null, null, "energySupplierGln", "balanceResponsiblePartyGln", AggregationLevel.EnergySupplierAndBalanceResponsibleAndGridArea },
            { null, null, null, "balanceResponsiblePartyGln", AggregationLevel.BalanceResponsibleAndGridArea },
        };
    }

    public static TheoryData<MeteringPointType?, SettlementMethod?, string?, string?, Type> InvalidAggregationLevelData()
    {
        return new TheoryData<MeteringPointType?, SettlementMethod?, string?, string?, Type>
        {
            { MeteringPointType.Exchange, null, "energySupplierGln", null, typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, null, null, "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, null, "energySupplierGln", "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.NonProfiled, "energySupplierGln", null, typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.NonProfiled, null, "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.NonProfiled, "energySupplierGln", "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.Flex, "energySupplierGln", null, typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.Flex, null, "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Exchange, SettlementMethod.Flex, "energySupplierGln", "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Consumption, null, "energySupplierGln", null, typeof(InvalidOperationException) },
            { MeteringPointType.Consumption, null, null, "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
            { MeteringPointType.Consumption, null, "energySupplierGln", "balanceResponsiblePartyGln", typeof(InvalidOperationException) },
        };
    }

    [Theory]
    [MemberData(nameof(AggregationLevelData))]
    public void Given_ValidInput_When_Mapping_Then_ReturnsExpectedAggregationLevel(
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        string? energySupplierGln,
        string? balanceResponsiblePartyGln,
        AggregationLevel expected)
    {
        // Act
        var actual = AggregationLevelMapper.Map(
            meteringPointType,
            settlementMethod,
            energySupplierGln,
            balanceResponsiblePartyGln);

        // Assert
        actual.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(InvalidAggregationLevelData))]
    public void Given_InvalidInput_When_Mapping_Then_ThrowsInvalidOperationException(
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        string? energySupplierGln,
        string? balanceResponsiblePartyGln,
        Type expectedException)
    {
        // Arrange & Act
        var act = () => (object?)AggregationLevelMapper.Map(
            meteringPointType,
            settlementMethod,
            energySupplierGln,
            balanceResponsiblePartyGln);

        // Assert
        Assert.Throws(expectedException, act);
    }
}
