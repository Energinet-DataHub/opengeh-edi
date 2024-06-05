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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using DomainModel = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;

public class EnergyResultMessageDtoFactory()
{
    public static EnergyResultMessageDto Create(
        DomainModel.EventId eventId,
        EnergyResultPerGridArea energyResult,
        DomainModel.ActorNumber receiverNumber)
    {
        ArgumentNullException.ThrowIfNull(energyResult);

        var receiverRole = DomainModel.ActorRole.MeteredDataResponsible;
        var (businessReason, settlementVersion) = MapToBusinessReasonAndSettlementVersion(energyResult.CalculationType);

        return EnergyResultMessageDto.Create(
            eventId,
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            gridAreaCode: energyResult.GridAreaCode,
            meteringPointType: energyResult.MeteringPointType.Name,
            settlementMethod: energyResult.SettlementMethod?.Name,
            measureUnitType: DomainModel.MeasurementUnit.Kwh.Name, // TODO: Should this be read from Databricks?
            resolution: energyResult.Resolution.Name,
            energySupplierNumber: null,
            balanceResponsibleNumber: null,
            period: new DomainModel.Period(energyResult.PeriodStartUtc, energyResult.PeriodEndUtc),
            points: CreateEnergyResultMessagePoints(energyResult.TimeSeriesPoints),
            businessReasonName: businessReason.Name,
            calculationResultVersion: energyResult.CalculationVersion,
            settlementVersion: settlementVersion?.Name);
    }

    // TODO: Move to mapper?
    private static IReadOnlyCollection<EnergyResultMessagePoint> CreateEnergyResultMessagePoints(EnergyTimeSeriesPoint[] timeSeriesPoints)
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

    // TODO: Move to mapper?
    private static DomainModel.CalculatedQuantityQuality MapToCalculatedQuantityQuality(IReadOnlyCollection<QuantityQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        return (
            missing: qualities.Contains(QuantityQuality.Missing),
            estimated: qualities.Contains(QuantityQuality.Estimated),
            measured: qualities.Contains(QuantityQuality.Measured),
            calculated: qualities.Contains(QuantityQuality.Calculated)) switch
        {
            (missing: true, estimated: false, measured: false, calculated: false) => DomainModel.CalculatedQuantityQuality.Missing,
            (missing: true, _, _, _) => DomainModel.CalculatedQuantityQuality.Incomplete,
            (_, estimated: true, _, _) => DomainModel.CalculatedQuantityQuality.Estimated,
            (_, _, measured: true, _) => DomainModel.CalculatedQuantityQuality.Measured,
            (_, _, _, calculated: true) => DomainModel.CalculatedQuantityQuality.Calculated,

            _ => DomainModel.CalculatedQuantityQuality.NotAvailable,
        };
    }

    // TODO: Move to mapper?
    private static (DomainModel.BusinessReason BusinessReason, DomainModel.SettlementVersion? SettlementVersion) MapToBusinessReasonAndSettlementVersion(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.Aggregation => (DomainModel.BusinessReason.PreliminaryAggregation, null),
            CalculationType.BalanceFixing => (DomainModel.BusinessReason.BalanceFixing, null),
            CalculationType.WholesaleFixing => (DomainModel.BusinessReason.WholesaleFixing, null),
            CalculationType.FirstCorrectionSettlement => (DomainModel.BusinessReason.Correction, SettlementVersion.FirstCorrection),
            CalculationType.SecondCorrectionSettlement => (DomainModel.BusinessReason.Correction, SettlementVersion.SecondCorrection),
            CalculationType.ThirdCorrectionSettlement => (DomainModel.BusinessReason.Correction, SettlementVersion.ThirdCorrection),

            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                actualValue: calculationType,
                "Value does not contain a valid calculation type."),
        };
    }
}
