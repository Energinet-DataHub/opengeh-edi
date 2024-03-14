﻿// Copyright 2020 Energinet DataHub A/S
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;
using NodaTime.Serialization.Protobuf;
using QuantityQuality = Energinet.DataHub.Edi.Responses.WholesaleServicesRequestSeries.Types.QuantityQuality;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;

public class WholesaleServicesRequestAcceptedMapper : IInboxEventMapper
{
    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(WholesaleServicesRequestAccepted), StringComparison.OrdinalIgnoreCase);
    }

    public Task<INotification> MapFromAsync(byte[] payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var wholesaleServicesRequestAccepted =
            WholesaleServicesRequestAccepted.Parser.ParseFrom(payload);

        ArgumentNullException.ThrowIfNull(wholesaleServicesRequestAccepted);

        var acceptedWholesaleServicesSeries = new List<AcceptedWholesaleServicesSerieDto>();
        foreach (var aggregation in wholesaleServicesRequestAccepted.Series)
        {
            acceptedWholesaleServicesSeries.Add(new AcceptedWholesaleServicesSerieDto(
                MapPoints(aggregation.TimeSeriesPoints),
                MapMeteringPointType(aggregation.MeteringPointType),
                MapResolution(aggregation.Resolution),
                MapChargeType(aggregation.ChargeType),
                MapQuantityUnit(aggregation.QuantityUnit),
                SettlementVersion: MapSettlementVersion(aggregation.CalculationType),
                MapSettlementMethod(aggregation.SettlementMethod),
                MapCurrency(aggregation.Currency),
                aggregation.ChargeOwnerId,
                aggregation.EnergySupplierId,
                aggregation.GridArea,
                aggregation.ChargeCode,
                aggregation.Period.StartOfPeriod.ToInstant(),
                aggregation.Period.EndOfPeriod.ToInstant(),
                aggregation.CalculationResultVersion));
        }

        return Task.FromResult<INotification>(new WholesaleServicesRequestWasAccepted(
            referenceId,
            acceptedWholesaleServicesSeries));
    }

    private static SettlementVersion MapSettlementVersion(WholesaleServicesRequestSeries.Types.CalculationType calculationType)
    {
        return calculationType switch
        {
            WholesaleServicesRequestSeries.Types.CalculationType.WholesaleFixing => SettlementVersion.WholesaleFixing,
            WholesaleServicesRequestSeries.Types.CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrectionSettlement,
            WholesaleServicesRequestSeries.Types.CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrectionSettlement,
            WholesaleServicesRequestSeries.Types.CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrectionSettlement,
            WholesaleServicesRequestSeries.Types.CalculationType.Unspecified => throw new InvalidOperationException("Could not map settlement version"),
            _ => throw new InvalidOperationException("Unknown calculation type"),
        };
    }

    private static Currency MapCurrency(WholesaleServicesRequestSeries.Types.Currency currency)
    {
        return currency switch
        {
            WholesaleServicesRequestSeries.Types.Currency.Dkk => Currency.Dkk,
            WholesaleServicesRequestSeries.Types.Currency.Unspecified => throw new InvalidOperationException("Could not map currency"),
            _ => throw new InvalidOperationException("Unknown currency type"),
        };
    }

    private static SettlementMethod? MapSettlementMethod(WholesaleServicesRequestSeries.Types.SettlementMethod settlementMethod)
    {
        return settlementMethod switch
        {
            WholesaleServicesRequestSeries.Types.SettlementMethod.Flex => SettlementMethod.Flex,
            WholesaleServicesRequestSeries.Types.SettlementMethod.NonProfiled => SettlementMethod.NonProfiled,
            WholesaleServicesRequestSeries.Types.SettlementMethod.Unspecified => null,
            _ => throw new InvalidOperationException("Unknown settlement method"),
        };
    }

    private static QuantityUnit MapQuantityUnit(WholesaleServicesRequestSeries.Types.QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            WholesaleServicesRequestSeries.Types.QuantityUnit.Kwh => QuantityUnit.Kwh,
            WholesaleServicesRequestSeries.Types.QuantityUnit.Pieces => QuantityUnit.Pieces,
            WholesaleServicesRequestSeries.Types.QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map quantity unit"),
            _ => throw new InvalidOperationException("Unknown quantity unit"),
        };
    }

    private static ChargeType MapChargeType(WholesaleServicesRequestSeries.Types.ChargeType chargeType)
    {
        return chargeType switch
        {
            WholesaleServicesRequestSeries.Types.ChargeType.Fee => ChargeType.Fee,
            WholesaleServicesRequestSeries.Types.ChargeType.Tariff => ChargeType.Tariff,
            WholesaleServicesRequestSeries.Types.ChargeType.Subscription => ChargeType.Subscription,
            WholesaleServicesRequestSeries.Types.ChargeType.Unspecified => throw new InvalidOperationException("Could not map charge type"),
            _ => throw new InvalidOperationException("Unknown charge type"),
        };
    }

    private static Resolution MapResolution(WholesaleServicesRequestSeries.Types.Resolution resolution)
    {
        return resolution switch
        {
            WholesaleServicesRequestSeries.Types.Resolution.Day => Resolution.Day,
            WholesaleServicesRequestSeries.Types.Resolution.Hour => Resolution.Hour,
            WholesaleServicesRequestSeries.Types.Resolution.Monthly => Resolution.Monthly,
            WholesaleServicesRequestSeries.Types.Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution"),
            _ => throw new InvalidOperationException("Unknown resolution"),
        };
    }

    private static MeteringPointType MapMeteringPointType(WholesaleServicesRequestSeries.Types.MeteringPointType meteringPointType)
    {
        return meteringPointType switch
        {
            WholesaleServicesRequestSeries.Types.MeteringPointType.Production => MeteringPointType.Production,
            WholesaleServicesRequestSeries.Types.MeteringPointType.Consumption => MeteringPointType.Consumption,
            WholesaleServicesRequestSeries.Types.MeteringPointType.Unspecified => throw new InvalidOperationException("Could not map metering point type"),
            _ => throw new InvalidOperationException("Unknown metering point type"),
        };
    }

    private static ReadOnlyCollection<Point> MapPoints(RepeatedField<WholesaleServicesRequestSeries.Types.Point> timeSeriesPoints)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints.OrderBy(x => x.Time))
        {
            points.Add(new Point(
                pointPosition,
                Parse(point.Quantity) ?? throw new InvalidOperationException("Missing time serie point quantity"),
                MapQuality(point.QuantityQualities.ToList().AsReadOnly()),
                Parse(point.Price),
                Parse(point.Amount)));

            pointPosition++;
        }

        return points.AsReadOnly();
    }

    private static CalculatedQuantityQuality MapQuality(IReadOnlyCollection<QuantityQuality> quantityQualities)
    {
        ArgumentNullException.ThrowIfNull(quantityQualities);

        return (missing: quantityQualities.Contains(QuantityQuality.Missing),
                estimated: quantityQualities.Contains(QuantityQuality.Estimated),
                measured: quantityQualities.Contains(QuantityQuality.Measured),
                calculated: quantityQualities.Contains(QuantityQuality.Calculated)) switch
            {
                (missing: true, _, _, _) => CalculatedQuantityQuality.Missing,
                (_, estimated: true, _, _) => CalculatedQuantityQuality.Estimated,
                (_, _, measured: true, _) => CalculatedQuantityQuality.Measured,
                (_, _, _, calculated: true) => CalculatedQuantityQuality.Calculated,
                _ => throw new InvalidOperationException("Unknown quantity quality type"),
            };
    }

    private static decimal? Parse(DecimalValue? input)
    {
        if (input is null)
        {
            return null;
        }

        const decimal nanoFactor = 1_000_000_000;
        return input.Units + (input.Nanos / nanoFactor);
    }
}
