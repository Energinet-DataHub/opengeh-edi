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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;

public static class RequestedCalculationTypeMapper
{
    public static CalculationType ToRequestedCalculationType(string businessReason, string? settlementVersion)
    {
        if (businessReason != DataHubNames.BusinessReason.Correction && settlementVersion != null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settlementVersion),
                settlementVersion,
                $"Value must be null when {nameof(businessReason)} is not {nameof(DataHubNames.BusinessReason.Correction)}.");
        }

        return businessReason switch
        {
            DataHubNames.BusinessReason.BalanceFixing => CalculationType.BalanceFixing,
            DataHubNames.BusinessReason.PreliminaryAggregation => CalculationType.Aggregation,
            DataHubNames.BusinessReason.WholesaleFixing => CalculationType.WholesaleFixing,
            DataHubNames.BusinessReason.Correction => settlementVersion switch
            {
                DataHubNames.SettlementVersion.FirstCorrection => CalculationType.FirstCorrectionSettlement,
                DataHubNames.SettlementVersion.SecondCorrection => CalculationType.SecondCorrectionSettlement,
                DataHubNames.SettlementVersion.ThirdCorrection => CalculationType.ThirdCorrectionSettlement,
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
