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

using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Currency = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Currency;
using QuantityUnit = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.QuantityUnit;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public static class WholesaleServicesResultMapper
{
    public static AcceptedWholesaleServicesSerieDto MapToAcceptedWholesaleServicesSerieDto(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.WholesaleServices wholesaleSeries)
    {
        return new AcceptedWholesaleServicesSerieDto(
            MapPoints(wholesaleSeries.TimeSeriesPoints, wholesaleSeries.Resolution, wholesaleSeries.ChargeType),
            MapMeteringPointType(wholesaleSeries),
            MapResolution(wholesaleSeries.Resolution),
            wholesaleSeries.ChargeType != null ? MapChargeType(wholesaleSeries.ChargeType.Value) : null,
            MapMeasurementUnit(wholesaleSeries),
            SettlementVersion: MapSettlementVersion(wholesaleSeries.CalculationType),
            MapSettlementMethod(wholesaleSeries.SettlementMethod),
            MapCurrency(wholesaleSeries.Currency),
            ShouldHaveChargeOwner(wholesaleSeries) ? ActorNumber.Create(wholesaleSeries.ChargeOwnerId!) : null,
            ActorNumber.Create(wholesaleSeries.EnergySupplierId),
            wholesaleSeries.GridArea,
            wholesaleSeries.ChargeCode,
            wholesaleSeries.Period.Start,
            wholesaleSeries.Period.End,
            wholesaleSeries.Version);
    }

    private static bool ShouldHaveChargeOwner(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.WholesaleServices wholesaleSeries)
    {
        /*
         * The charge owner should be present on the outgoing message
         * if there is a charge owner and the message is not a total sum.
         * Note that some total sums do have a charge owner (the sums for a specific charge owner) while others do not
         * (those for an energy supplier).
         * However, no total sum has a quantity unit as this is absent from the underlying data source
         * and can thus be used to determine if the message is a total sum.
         * In other words: if the message has a quantity unit, it is not a total sum.
         */
        return wholesaleSeries is { ChargeOwnerId: not null, QuantityUnit: not null };
    }

    private static BuildingBlocks.Domain.Models.MeteringPointType? MapMeteringPointType(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.WholesaleServices wholesaleSeries)
    {
        // Monthly sum does not have a metering point type
        if (wholesaleSeries.MeteringPointType != null)
            return MeteringPointTypeMapper.Map(wholesaleSeries.MeteringPointType.Value);
        return null;
    }

    private static SettlementVersion? MapSettlementVersion(
        CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.WholesaleFixing => null,
            CalculationType.FirstCorrectionSettlement => SettlementVersion
                .FirstCorrection,
            CalculationType.SecondCorrectionSettlement => SettlementVersion
                .SecondCorrection,
            CalculationType.ThirdCorrectionSettlement => SettlementVersion
                .ThirdCorrection,
            _ => throw new InvalidOperationException("Unknown calculation type"),
        };
    }

    private static Currency MapCurrency(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Currency currency)
    {
        return currency switch
        {
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Currency.DKK => Currency.DanishCrowns,
            _ => throw new InvalidOperationException("Unknown currency type"),
        };
    }

    private static BuildingBlocks.Domain.Models.SettlementMethod? MapSettlementMethod(
        OutgoingMessages.Interfaces.Models.CalculationResults.SettlementMethod? settlementMethod)
    {
        return settlementMethod switch
        {
            OutgoingMessages.Interfaces.Models.CalculationResults.SettlementMethod.Flex => BuildingBlocks.Domain.Models.SettlementMethod.Flex,
            OutgoingMessages.Interfaces.Models.CalculationResults.SettlementMethod.NonProfiled => BuildingBlocks.Domain.Models.SettlementMethod.NonProfiled,
            null => null,
            _ => throw new InvalidOperationException("Unknown settlement method"),
        };
    }

    private static MeasurementUnit MapMeasurementUnit(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.WholesaleServices wholesaleServicesRequest)
    {
        if (wholesaleServicesRequest is
            { QuantityUnit: not null, Resolution: OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution.Month })
        {
            return MeasurementUnit.Kwh;
        }

        return wholesaleServicesRequest.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh,
            QuantityUnit.Pieces => MeasurementUnit.Pieces,
            _ => throw new InvalidOperationException("Unknown quantity unit"),
        };
    }

    private static ChargeType MapChargeType(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType chargeType)
    {
        return chargeType switch
        {
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Fee => ChargeType.Fee,
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Tariff => ChargeType.Tariff,
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Subscription => ChargeType.Subscription,
            _ => throw new InvalidOperationException("Unknown charge type"),
        };
    }

    private static Resolution MapResolution(OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution resolution)
    {
        return resolution switch
        {
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution.Day => Resolution.Daily,
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution.Hour => Resolution.Hourly,
            OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution.Month => Resolution.Monthly,
            _ => throw new InvalidOperationException("Unknown resolution"),
        };
    }

    private static ReadOnlyCollection<Point> MapPoints(
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints,
        OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.Resolution resolution,
        OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType? chargeType)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints.OrderBy(x => x.Time))
        {
            var price = point.Price;
            points.Add(new Point(
                pointPosition,
                point.Quantity,
                CalculatedQuantityQualityMapper.MapForWholesaleServices(
                    point.Qualities?.ToList().AsReadOnly(),
                    resolution,
                    price != null,
                    chargeType),
                price,
                point.Amount));

            pointPosition++;
        }

        return points.AsReadOnly();
    }
}
