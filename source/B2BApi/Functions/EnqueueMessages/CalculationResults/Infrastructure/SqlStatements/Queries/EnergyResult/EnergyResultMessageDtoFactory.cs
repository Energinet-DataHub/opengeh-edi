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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Interfaces.Model;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Interfaces.Model.EnergyResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.Model;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using DomainModel = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Infrastructure.SqlStatements.Queries.EnergyResult;

public class EnergyResultMessageDtoFactory
{
    private readonly IMasterDataClient _masterDataClient;

    public EnergyResultMessageDtoFactory(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public async Task<EnergyResultMessageDto> CreateAsync(
        DomainModel.EventId eventId,
        EnergyResultPerGridArea energyResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(energyResult);

        var receiverRole = DomainModel.ActorRole.MeteredDataResponsible;
        var receiverNumber = await _masterDataClient
            .GetGridOwnerForGridAreaCodeAsync(energyResult.GridAreaCode, cancellationToken).ConfigureAwait(false);

        return EnergyResultMessageDto.Create(
            eventId,
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            gridAreaCode: energyResult.GridAreaCode,
            meteringPointType: MapToDomainMeteringPointType(energyResult.MeteringPointType).Name,
            settlementMethod: MapToDomainSettlementMethodType(energyResult.SettlementMethod)?.Name,
            measureUnitType: DomainModel.MeasurementUnit.Kwh.Name, // TODO: Should this be read from Databricks?
            resolution: MapToDomainResolution(energyResult.Resolution).Name,
            energySupplierNumber: null,
            balanceResponsibleNumber: null,
            period: new DomainModel.Period(energyResult.PeriodStartUtc, energyResult.PeriodEndUtc),
            points: MapToOutgoingPoints(energyResult.TimeSeriesPoints),
            businessReasonName: MapToDomainBusinessReason(energyResult.CalculationType).Name,
            calculationResultVersion: energyResult.CalculationVersion,
            settlementVersion: MapToDomainSettlementVersion(energyResult.CalculationType)?.Name);
    }

    // TODO: Move to mapper?
    private static DomainModel.MeteringPointType MapToDomainMeteringPointType(MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            // Metering point types
            MeteringPointType.Production => DomainModel.MeteringPointType.Production,
            MeteringPointType.Consumption => DomainModel.MeteringPointType.Consumption,
            MeteringPointType.Exchange => DomainModel.MeteringPointType.Exchange,

            // Child metering point types
            MeteringPointType.VeProduction => DomainModel.MeteringPointType.VeProduction,
            MeteringPointType.NetProduction => DomainModel.MeteringPointType.NetProduction,
            MeteringPointType.SupplyToGrid => DomainModel.MeteringPointType.SupplyToGrid,
            MeteringPointType.ConsumptionFromGrid => DomainModel.MeteringPointType.ConsumptionFromGrid,
            MeteringPointType.WholesaleServicesInformation => DomainModel.MeteringPointType.WholesaleServicesInformation,
            MeteringPointType.OwnProduction => DomainModel.MeteringPointType.OwnProduction,
            MeteringPointType.NetFromGrid => DomainModel.MeteringPointType.NetFromGrid,
            MeteringPointType.NetToGrid => DomainModel.MeteringPointType.NetToGrid,
            MeteringPointType.TotalConsumption => DomainModel.MeteringPointType.TotalConsumption,
            MeteringPointType.ElectricalHeating => DomainModel.MeteringPointType.ElectricalHeating,
            MeteringPointType.NetConsumption => DomainModel.MeteringPointType.NetConsumption,
            MeteringPointType.EffectSettlement => DomainModel.MeteringPointType.EffectSettlement,

            _ => throw new ArgumentOutOfRangeException(
                nameof(meteringPointType),
                actualValue: meteringPointType,
                "Value does not contain a valid metering point type."),
        };
    }

    // TODO: Move to mapper?
    private static DomainModel.SettlementMethod? MapToDomainSettlementMethodType(SettlementMethod? settlementMethod)
    {
        return settlementMethod switch
        {
            SettlementMethod.Flex => DomainModel.SettlementMethod.Flex,
            SettlementMethod.NonProfiled => DomainModel.SettlementMethod.NonProfiled,
            null => null,

            _ => throw new ArgumentOutOfRangeException(
                nameof(settlementMethod),
                actualValue: settlementMethod,
                "Value does not contain a valid settlement method."),
        };
    }

    // TODO: Move to mapper?
    private DomainModel.Resolution MapToDomainResolution(EnergyResultResolution resolution)
    {
        return resolution switch
        {
            EnergyResultResolution.Quarter => DomainModel.Resolution.QuarterHourly,
            EnergyResultResolution.Hour => DomainModel.Resolution.Hourly,

            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution),
                actualValue: resolution,
                "Value does not contain a valid resolution."),
        };
    }

    // TODO: Move to mapper?
    private IReadOnlyCollection<EnergyResultMessagePoint> MapToOutgoingPoints(EnergyTimeSeriesPoint[] timeSeriesPoints)
    {
        ArgumentNullException.ThrowIfNull(timeSeriesPoints);

        return timeSeriesPoints
            .Select(
                (p, index) => new EnergyResultMessagePoint(
                    index + 1, // Position starts at 1, so position = index + 1
                    p.Quantity,
                    MapToDomainQuality(p.Qualities),
                    p.TimeUtc.ToString()))
            .ToList()
            .AsReadOnly();
    }

    // TODO: Move to mapper?
    private DomainModel.CalculatedQuantityQuality MapToDomainQuality(IReadOnlyCollection<QuantityQuality> qualities)
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
    private DomainModel.BusinessReason MapToDomainBusinessReason(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.Aggregation => DomainModel.BusinessReason.PreliminaryAggregation,
            CalculationType.BalanceFixing => DomainModel.BusinessReason.BalanceFixing,
            CalculationType.WholesaleFixing => DomainModel.BusinessReason.WholesaleFixing,
            CalculationType.FirstCorrectionSettlement => DomainModel.BusinessReason.Correction,
            CalculationType.SecondCorrectionSettlement => DomainModel.BusinessReason.Correction,
            CalculationType.ThirdCorrectionSettlement => DomainModel.BusinessReason.Correction,

            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                actualValue: calculationType,
                "Value does not contain a valid calculation type."),
        };
    }

    // TODO: Move to mapper?
    private DomainModel.SettlementVersion? MapToDomainSettlementVersion(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.BalanceFixing or CalculationType.Aggregation or CalculationType.WholesaleFixing => null,
            CalculationType.FirstCorrectionSettlement => DomainModel.SettlementVersion.FirstCorrection,
            CalculationType.SecondCorrectionSettlement => DomainModel.SettlementVersion.SecondCorrection,
            CalculationType.ThirdCorrectionSettlement => DomainModel.SettlementVersion.ThirdCorrection,

            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                calculationType,
                "Value does not contain a valid calculation type."),
        };
    }
}
