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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;

public static class RequestedCalculationTypeMapper
{
    public static CalculationType ToRequestedCalculationType(string businessReason, string? settlementVersion)
    {
        if (businessReason != BusinessReason.Correction.Name && settlementVersion != null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settlementVersion),
                settlementVersion,
                $"Value must be null when {nameof(businessReason)} is not {nameof(BusinessReason.Correction)}.");
        }

        return businessReason switch
        {
            var br when br == BusinessReason.BalanceFixing.Name => CalculationType.BalanceFixing,
            var br when br == BusinessReason.PreliminaryAggregation.Name => CalculationType.Aggregation,
            var br when br == BusinessReason.WholesaleFixing.Name => CalculationType.WholesaleFixing,
            var br when br == BusinessReason.Correction.Name => settlementVersion switch
            {
                var sm when sm == SettlementVersion.FirstCorrection.Name => CalculationType.FirstCorrectionSettlement,
                var sm when sm == SettlementVersion.SecondCorrection.Name => CalculationType.SecondCorrectionSettlement,
                var sm when sm == SettlementVersion.ThirdCorrection.Name => CalculationType.ThirdCorrectionSettlement,
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
