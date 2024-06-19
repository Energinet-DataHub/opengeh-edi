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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;

public static class WholesaleResultMessageDtoFactory
{
    public static WholesaleServicesMessageDto Create(
        EventId eventId,
        WholesaleAmountPerCharge wholesaleResult,
        ActorNumber gridAreaOwner)
    {
        ArgumentNullException.ThrowIfNull(wholesaleResult);

        var (businessReason, settlementVersion) =
            MapToBusinessReasonAndSettlementVersion(wholesaleResult.CalculationType);
        var message = CreateWholesaleResultSeries(wholesaleResult, settlementVersion);

        var chargeOwner = GetChargeOwnerReceiver(
            gridAreaOwner,
            ActorNumber.Create(wholesaleResult.ChargeOwnerId),
            wholesaleResult.IsTax);

        return WholesaleServicesMessageDto.Create(
            eventId,
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            chargeOwnerId: chargeOwner,
            businessReason: businessReason.Name,
            wholesaleSeries: message);
    }

    public static WholesaleServicesMessageDto Create(
        EventId eventId,
        WholesaleMonthlyAmountPerCharge wholesaleResult,
        ActorNumber gridAreaOwner)
    {
        ArgumentNullException.ThrowIfNull(wholesaleResult);

        var (businessReason, settlementVersion) =
            MapToBusinessReasonAndSettlementVersion(wholesaleResult.CalculationType);
        var message = CreateWholesaleResultSeries(wholesaleResult, settlementVersion);

        var chargeOwner = GetChargeOwnerReceiver(
            gridAreaOwner,
            ActorNumber.Create(wholesaleResult.ChargeOwnerId),
            wholesaleResult.IsTax);

        return WholesaleServicesMessageDto.Create(
            eventId,
            receiverNumber: message.EnergySupplier,
            receiverRole: ActorRole.EnergySupplier,
            chargeOwnerId: chargeOwner,
            businessReason: businessReason.Name,
            wholesaleSeries: message);
    }

    private static WholesaleServicesSeries CreateWholesaleResultSeries(
        WholesaleAmountPerCharge result,
        SettlementVersion? settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: result.CalculationVersion,
            GridAreaCode: result.GridAreaCode,
            ChargeCode: result.ChargeCode,
            IsTax: result.IsTax,
            Points: PointsBasedOnChargeType(result.TimeSeriesPoints, result.ChargeType),
            EnergySupplier: ActorNumber.Create(result.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(result.ChargeOwnerId),
            Period: new Period(result.PeriodStartUtc, result.PeriodEndUtc),
            SettlementVersion: settlementVersion,
            QuantityMeasureUnit: result.QuantityUnit,
            null,
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: result.Currency,
            ChargeType: result.ChargeType,
            Resolution: result.Resolution,
            MeteringPointType: result.MeteringPointType,
            null,
            SettlementMethod: result.SettlementMethod);
    }

    private static WholesaleServicesSeries CreateWholesaleResultSeries(
        WholesaleMonthlyAmountPerCharge result,
        SettlementVersion? settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: result.CalculationVersion,
            GridAreaCode: result.GridAreaCode,
            ChargeCode: result.ChargeCode,
            IsTax: result.IsTax,
            Points: PointsBasedOnChargeType(result.TimeSeriesPoints, result.ChargeType),
            EnergySupplier: ActorNumber.Create(result.EnergySupplierId),
            ChargeOwner: ActorNumber.Create(result.ChargeOwnerId),
            Period: new Period(result.PeriodStartUtc, result.PeriodEndUtc),
            SettlementVersion: settlementVersion,
            QuantityMeasureUnit: result.QuantityUnit,
            null,
            PriceMeasureUnit: MeasurementUnit.Kwh,
            Currency: result.Currency,
            ChargeType: result.ChargeType,
            Resolution: result.Resolution,
            MeteringPointType: null,
            null,
            SettlementMethod: null);
    }

    private static IReadOnlyCollection<WholesaleServicesPoint> PointsBasedOnChargeType(
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints,
        ChargeType chargeType)
    {
        return timeSeriesPoints
        .Select(
            (p, index) => new WholesaleServicesPoint(
                index + 1, // Position starts at 1, so position = index + 1
                p.Quantity,
                p.Price,
                p.Amount,
                GetQuantityQuality(p.Price, p.Qualities, chargeType)))
        .ToList()
        .AsReadOnly();
    }

    /// <summary>
    /// Quantity quality mappings is defined by the business.
    /// See "https://energinet.atlassian.net/wiki/spaces/D3/pages/529989633/QuantityQuality" for more information.
    /// </summary>
    private static CalculatedQuantityQuality GetQuantityQuality(decimal? price, IReadOnlyCollection<QuantityQuality> qualities, ChargeType? chargeType)
    {
        if (price == null)
        {
            return CalculatedQuantityQuality.Missing;
        }

        if (chargeType == ChargeType.Subscription || chargeType == ChargeType.Fee)
        {
            return CalculatedQuantityQuality.Calculated;
        }

        return MapQuantityQualitiesToQuality(qualities);
    }

    private static CalculatedQuantityQuality MapQuantityQualitiesToQuality(
        IReadOnlyCollection<QuantityQuality> qualities)
    {
        ArgumentNullException.ThrowIfNull(qualities);

        return (missing: qualities.Contains(QuantityQuality.Missing),
                estimated: qualities.Contains(QuantityQuality.Estimated),
                measured: qualities.Contains(QuantityQuality.Measured),
                calculated: qualities.Contains(QuantityQuality.Calculated)) switch
            {
                (missing: true, estimated: false, measured: false, calculated: false) => CalculatedQuantityQuality.Missing,
                (missing: true, _, _, _) => CalculatedQuantityQuality.Incomplete,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Calculated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Calculated,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => CalculatedQuantityQuality.NotAvailable,
            };
    }

    private static ActorNumber GetChargeOwnerReceiver(ActorNumber gridAreaOwner, ActorNumber chargeOwner, bool isTax)
    {
        return isTax
            ? gridAreaOwner
            : chargeOwner;
    }

    private static (BusinessReason BusinessReason, SettlementVersion? SettlementVersion)
        MapToBusinessReasonAndSettlementVersion(
            CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.Aggregation => (BusinessReason.PreliminaryAggregation, null),
            CalculationType.BalanceFixing => (BusinessReason.BalanceFixing, null),
            CalculationType.WholesaleFixing => (BusinessReason.WholesaleFixing, null),
            CalculationType.FirstCorrectionSettlement => (BusinessReason.Correction, SettlementVersion.FirstCorrection),
            CalculationType.SecondCorrectionSettlement => (BusinessReason.Correction,
                SettlementVersion.SecondCorrection),
            CalculationType.ThirdCorrectionSettlement => (BusinessReason.Correction, SettlementVersion.ThirdCorrection),

            _ => throw new ArgumentOutOfRangeException(
                nameof(calculationType),
                actualValue: calculationType,
                "Value does not contain a valid calculation type."),
        };
    }
}
