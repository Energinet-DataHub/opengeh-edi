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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects;
using Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.DeltaTableMappers;

public class BusinessReasonAndSettlementVersionMapperTests
{
    public static IEnumerable<object?[]> GetValidCalculationType()
    {
        return
        [
            [DeltaTableCalculationType.Aggregation, BusinessReason.PreliminaryAggregation, null],
            [DeltaTableCalculationType.BalanceFixing, BusinessReason.BalanceFixing, null],
            [DeltaTableCalculationType.WholesaleFixing, BusinessReason.WholesaleFixing, null],
            [DeltaTableCalculationType.FirstCorrectionSettlement, BusinessReason.Correction, SettlementVersion.FirstCorrection],
            [DeltaTableCalculationType.SecondCorrectionSettlement, BusinessReason.Correction, SettlementVersion.SecondCorrection],
            [DeltaTableCalculationType.ThirdCorrectionSettlement, BusinessReason.Correction, SettlementVersion.ThirdCorrection],
        ];
    }

    [Theory]
    [MemberData(nameof(GetValidCalculationType))]
    public void FromDeltaTableValue_ValidInputs_ReturnsExpectedValue(
        string calculationType,
        BusinessReason expectedBusinessReason,
        SettlementVersion? expectedSettlementVersion)
    {
        // Act
        var (actualBusinessReason, actualSettlementVersion) =
            BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(calculationType);

        // Assert
        Assert.Equivalent(expectedBusinessReason, actualBusinessReason);
        Assert.Equivalent(expectedSettlementVersion, actualSettlementVersion);
    }

    [Fact]
    public void FromDeltaTableValue_InvalidCalculationType_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidCalculationType = "InvalidType";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(invalidCalculationType));
    }
}
