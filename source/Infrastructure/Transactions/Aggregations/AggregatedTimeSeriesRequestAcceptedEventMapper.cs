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
using Point = Domain.Transactions.AggregatedMeasureData.Point;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    public Task<INotification> MapFromAsync(string payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var aggregation =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseJson(payload);

        var aggregatiedTimeSerie = new AggregatedTimeSerie(
                MapPoints(aggregation.TimeSeriesPoints),
                MapMeteringPointType(aggregation),
                MapUnitType(aggregation),
                MapResolution(aggregation),
                MapPeriod(aggregation),
                MapGridAreaDetails(aggregation),
                MapSettlementVersion(aggregation));

        return Task.FromResult<INotification>(new AggregatedTimeSerieRequestWasAccepted(
            referenceId,
            aggregatiedTimeSerie));
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

    private static string? MapSettlementVersion(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return aggregation.SettlementVersion;
    }

    private static string MapMeteringPointType(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return aggregation.TimeSeriesType switch
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

    private static Domain.Transactions.AggregatedMeasureData.Period MapPeriod(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return new Domain.Transactions.AggregatedMeasureData.Period(aggregation.Period.StartOfPeriod.ToInstant(), aggregation.Period.EndOfPeriod.ToInstant());
    }

    private static string MapResolution(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return aggregation.Period.Resolution switch
        {
            Resolution.Pt15M => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
            Resolution.Pt1H => Domain.Transactions.Aggregations.Resolution.Hourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapUnitType(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return aggregation.QuantityUnit switch
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

    private static GridAreaDetails MapGridAreaDetails(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        var gridOperatorNumber = GridAreaLookup.GetGridOperatorFor(aggregation.GridArea);

        return new GridAreaDetails(aggregation.GridArea, gridOperatorNumber.Value);
    }
}
