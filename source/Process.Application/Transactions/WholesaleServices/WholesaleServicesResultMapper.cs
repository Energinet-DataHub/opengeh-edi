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
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers;
using Energinet.DataHub.Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults;
using Energinet.DataHub.Wholesale.Common.Interfaces.Models;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Currency = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Currency;
using QuantityUnit = Energinet.DataHub.Wholesale.CalculationResults.Interfaces.CalculationResults.QuantityUnit;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public static class WholesaleServicesResultMapper
{
    public static AcceptedWholesaleServicesSerieDto MapToAcceptedWholesaleServicesSerieDto(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.WholesaleServices wholesaleSeries)
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

    private static bool ShouldHaveChargeOwner(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.WholesaleServices wholesaleSeries)
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

    private static MeteringPointType? MapMeteringPointType(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.WholesaleServices wholesaleSeries)
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

    private static Currency MapCurrency(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Currency currency)
    {
        return currency switch
        {
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Currency.DKK => Currency.DanishCrowns,
            _ => throw new InvalidOperationException("Unknown currency type"),
        };
    }

    private static SettlementMethod? MapSettlementMethod(
        Wholesale.CalculationResults.Interfaces.CalculationResults.Model.SettlementMethod? settlementMethod)
    {
        return settlementMethod switch
        {
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.SettlementMethod.Flex => SettlementMethod.Flex,
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.SettlementMethod.NonProfiled => SettlementMethod.NonProfiled,
            null => null,
            _ => throw new InvalidOperationException("Unknown settlement method"),
        };
    }

    private static MeasurementUnit MapMeasurementUnit(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.WholesaleServices wholesaleServicesRequest)
    {
        if (wholesaleServicesRequest is
            { QuantityUnit: not null, Resolution: Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution.Month })
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

    private static ChargeType MapChargeType(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.ChargeType chargeType)
    {
        return chargeType switch
        {
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.ChargeType.Fee => ChargeType.Fee,
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.ChargeType.Tariff => ChargeType.Tariff,
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.ChargeType.Subscription => ChargeType.Subscription,
            _ => throw new InvalidOperationException("Unknown charge type"),
        };
    }

    private static Resolution MapResolution(Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution resolution)
    {
        return resolution switch
        {
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution.Day => Resolution.Daily,
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution.Hour => Resolution.Hourly,
            Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution.Month => Resolution.Monthly,
            _ => throw new InvalidOperationException("Unknown resolution"),
        };
    }

    private static ReadOnlyCollection<Point> MapPoints(
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints,
        Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.Resolution resolution,
        Wholesale.CalculationResults.Interfaces.CalculationResults.Model.WholesaleResults.ChargeType? chargeType)
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
