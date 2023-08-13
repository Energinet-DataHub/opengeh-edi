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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.OutgoingMessages;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using Infrastructure.InboxEvents;
using Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using MediatR;
using NodaTime.Serialization.Protobuf;
using GridAreaDetails = Domain.Transactions.AggregatedMeasureData.GridAreaDetails;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Point = Domain.Transactions.AggregatedMeasureData.Point;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;
using Serie = Energinet.DataHub.Edi.Responses.Serie;

namespace Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    private readonly IGridAreaLookup _gridAreaLookup;

    public AggregatedTimeSeriesRequestAcceptedEventMapper(
        IGridAreaLookup gridAreaLookup)
    {
        _gridAreaLookup = gridAreaLookup;
    }

    public async Task<INotification> MapFromAsync(string payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var inboxEvent =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseJson(payload);

        var aggregatedTimeSeries = new List<AggregatedTimeSerie>();

        foreach (var serie in inboxEvent.Series)
        {
            aggregatedTimeSeries.Add(
                new AggregatedTimeSerie(
                    MapPoints(serie.TimeSeriesPoints),
                    MapMeteringPointType(serie),
                    MapUnitType(serie),
                    MapResolution(serie.Period.Resolution),
                    MapPeriod(serie.Period),
                    await MapGridAreaDetailsAsync(serie).ConfigureAwait(false),
                    MapProduct(serie),
                    MapSettlementVersion(serie)));
        }

        return new AggregatedTimeSeriesRequestWasAccepted(
            referenceId,
            aggregatedTimeSeries);
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestAccepted), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var inboxEvent = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(
            payload);
        return inboxEvent.ToString();
    }

    private static string? MapSettlementVersion(Serie serie)
    {
        return serie.SettlementVersion;
    }

    private static string? MapProduct(Serie serie)
    {
        return serie.Product switch
        {
            Product.Energy => ProductType.Energy.Name,
            Product.Tarif => ProductType.Tarif.Name,
            Product.Unspecified => null,
            _ => throw new InvalidOperationException("Could not determine product type"),
        };
    }

    private static string MapMeteringPointType(Serie serie)
    {
        return serie.TimeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production.Name,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new InvalidOperationException("Could not determine metering point type"),
        };
    }

    private static IReadOnlyList<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints)
        {
            points.Add(new Point(pointPosition, Parse(point.Quantity), MapQuality(point.QuantityQuality), point.Time.ToString()));
            pointPosition++;
        }

        return points.AsReadOnly();
    }

    private static Domain.Transactions.AggregatedMeasureData.Period MapPeriod(Period period)
    {
        return new Domain.Transactions.AggregatedMeasureData.Period(period.StartOfPeriod.ToInstant(), period.EndOfPeriod.ToInstant());
    }

    private static string MapResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Pt15M => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
            Resolution.Pt1H => Domain.Transactions.Aggregations.Resolution.Hourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapUnitType(Serie serie)
    {
        return serie.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static string MapQuality(QuantityQuality quality)
    {
        return quality switch
        {
            QuantityQuality.Incomplete => Quality.Incomplete.Name,
            QuantityQuality.Measured => Quality.Measured.Name,
            QuantityQuality.Missing => Quality.Missing.Name,
            QuantityQuality.Estimated => Quality.Estimated.Name,
            QuantityQuality.Unspecified => throw new InvalidOperationException("Quality is not specified"),
            _ => throw new InvalidOperationException("Unknown quality type"),
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

    private async Task<GridAreaDetails> MapGridAreaDetailsAsync(Serie serie)
    {
        var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(serie.GridArea).ConfigureAwait(false);

        return new GridAreaDetails(serie.GridArea, gridOperatorNumber.Value);
    }
}
