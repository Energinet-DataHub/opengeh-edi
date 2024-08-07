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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;

public class EnergyResultMessageDtoFactory()
{
    public static IReadOnlyCollection<EnergyResultMessagePoint> CreateEnergyResultMessagePoints(IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        ArgumentNullException.ThrowIfNull(timeSeriesPoints);

        return timeSeriesPoints
            .Select(
                (p, index) => new EnergyResultMessagePoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    p.Quantity,
                    MapToCalculatedQuantityQuality(p.Qualities),
                    p.TimeUtc.ToString()))
            .ToList()
            .AsReadOnly();
    }

    public static (BusinessReason BusinessReason, SettlementVersion? SettlementVersion) MapToBusinessReasonAndSettlementVersion(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.Aggregation => (BusinessReason.PreliminaryAggregation, null),
            CalculationType.BalanceFixing => (BusinessReason.BalanceFixing, null),
            CalculationType.WholesaleFixing => (BusinessReason.WholesaleFixing, null),
            CalculationType.FirstCorrectionSettlement => (BusinessReason.Correction, SettlementVersion.FirstCorrection),
            CalculationType.SecondCorrectionSettlement => (BusinessReason.Correction, SettlementVersion.SecondCorrection),
            CalculationType.ThirdCorrectionSettlement => (BusinessReason.Correction, SettlementVersion.ThirdCorrection),

            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                actualValue: calculationType,
                "Value does not contain a valid calculation type."),
        };
    }

    private static CalculatedQuantityQuality MapToCalculatedQuantityQuality(IReadOnlyCollection<QuantityQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        return (
            missing: qualities.Contains(QuantityQuality.Missing),
            estimated: qualities.Contains(QuantityQuality.Estimated),
            measured: qualities.Contains(QuantityQuality.Measured),
            calculated: qualities.Contains(QuantityQuality.Calculated)) switch
        {
            (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
            (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
            (_, estimated: true, _, _) => CalculatedQuantityQuality.Estimated,
            (_, _, measured: true, _) => CalculatedQuantityQuality.Measured,
            (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,

            _ => CalculatedQuantityQuality.NotAvailable,
        };
    }
}
