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

public class CalculationTypeMapperTests
{
    public static IEnumerable<object?[]> GetValidBusinessReasonAndSettlementVersionCombination()
    {
        return
        [
            [BusinessReason.BalanceFixing, null, DeltaTableCalculationType.BalanceFixing],
            [BusinessReason.PreliminaryAggregation, null, DeltaTableCalculationType.Aggregation],
            [BusinessReason.WholesaleFixing, null, DeltaTableCalculationType.WholesaleFixing],
            [BusinessReason.Correction, SettlementVersion.FirstCorrection, DeltaTableCalculationType.FirstCorrectionSettlement],
            [BusinessReason.Correction, SettlementVersion.SecondCorrection, DeltaTableCalculationType.SecondCorrectionSettlement],
            [BusinessReason.Correction, SettlementVersion.ThirdCorrection, DeltaTableCalculationType.ThirdCorrectionSettlement],
        ];
    }

    public static IEnumerable<object?[]> GetInvalidBusinessReasonAndSettlementVersionCombination()
    {
        return
        [
            // Not allowed business reasons with settlement versions
            [BusinessReason.BalanceFixing, SettlementVersion.FirstCorrection, typeof(ArgumentOutOfRangeException)],
            [BusinessReason.PreliminaryAggregation, SettlementVersion.FirstCorrection, typeof(ArgumentOutOfRangeException)],
            [BusinessReason.WholesaleFixing, SettlementVersion.FirstCorrection, typeof(ArgumentOutOfRangeException)],
            // Not allowed business reasons without settlement versions
            [BusinessReason.Correction, null, typeof(ArgumentOutOfRangeException)],
            // Invalid business reasons
            [BusinessReason.PeriodicMetering, null, typeof(ArgumentOutOfRangeException)],
            [BusinessReason.PeriodicFlexMetering, null, typeof(ArgumentOutOfRangeException)],
            [BusinessReason.MoveIn, null, typeof(ArgumentOutOfRangeException)],
        ];
    }

    public static IEnumerable<object?[]> GetInvalidCalculationType()
    {
        var allBusinessReasons = EnumerationType
            .GetAll<BusinessReason>()
            .ToList();
        var allSettlementVersions = EnumerationType
            .GetAll<SettlementVersion>()
            .ToList();

        return allBusinessReasons
            .SelectMany(businessReason =>
                allSettlementVersions.Select(settlementVersion =>
                    new object?[] { businessReason, settlementVersion }))
            .Except(BusinessReasonAndSettlementVersionMapperTests.GetValidCalculationType())
            .ToList();
    }

    [Theory]
    [MemberData(nameof(GetValidBusinessReasonAndSettlementVersionCombination))]
    public void ToDeltaTableValue_ValidInputs_ReturnsExpectedValue(
        BusinessReason businessReason,
        SettlementVersion? settlementVersion,
        string expectedValue)
    {
        // Act
        var actualValue = CalculationTypeMapper.ToDeltaTableValue(businessReason, settlementVersion);

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [MemberData(nameof(GetInvalidBusinessReasonAndSettlementVersionCombination))]
    public void ToDeltaTableValue_InvalidBusinessReason_ThrowsArgumentOutOfRangeException(
        BusinessReason businessReason,
        SettlementVersion? settlementVersion,
        Type expectedExceptionType)
    {
        // Act & Assert
        Assert.Throws(
            expectedExceptionType,
            () => CalculationTypeMapper.ToDeltaTableValue(businessReason, settlementVersion));
    }
}
