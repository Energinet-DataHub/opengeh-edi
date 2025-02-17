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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;

public static class CalculationTypeMapper
{
    public static string ToDeltaTableValue(BusinessReason businessReason, SettlementVersion? settlementVersion)
    {
        if (businessReason != BusinessReason.Correction && settlementVersion != null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settlementVersion),
                settlementVersion,
                $"Value must be null when {nameof(businessReason)} is not {nameof(BusinessReason.Correction)}.");
        }

        return businessReason switch
        {
            var br when br == BusinessReason.BalanceFixing => DeltaTableCalculationType.BalanceFixing,
            var br when br == BusinessReason.PreliminaryAggregation => DeltaTableCalculationType.Aggregation,
            var br when br == BusinessReason.WholesaleFixing => DeltaTableCalculationType.WholesaleFixing,
            var br when br == BusinessReason.Correction => settlementVersion switch
            {
                var sm when sm == SettlementVersion.FirstCorrection => DeltaTableCalculationType.FirstCorrectionSettlement,
                var sm when sm == SettlementVersion.SecondCorrection => DeltaTableCalculationType.SecondCorrectionSettlement,
                var sm when sm == SettlementVersion.ThirdCorrection => DeltaTableCalculationType.ThirdCorrectionSettlement,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(settlementVersion),
                    settlementVersion,
                    $"Value cannot be mapped to a {nameof(RequestedCalculationType)}."),
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(businessReason),
                businessReason,
                $"Value cannot be mapped to a {nameof(RequestedCalculationType)}."),
        };
    }
}
