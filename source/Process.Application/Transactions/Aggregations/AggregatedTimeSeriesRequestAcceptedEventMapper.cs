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
using Energinet.DataHub.EDI.Application.GridAreas;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;
using GridAreaDetails = Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.GridAreaDetails;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    private readonly IGridAreaRepository _gridAreaRepository;

    public AggregatedTimeSeriesRequestAcceptedEventMapper(IGridAreaRepository gridAreaRepository)
    {
        _gridAreaRepository = gridAreaRepository;
    }

    public async Task<INotification> MapFromAsync(string payload, Guid referenceId, CancellationToken cancellationToken)
    {
        var aggregations =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseJson(payload);

        ArgumentNullException.ThrowIfNull(aggregations);

        var aggregatedTimeSeries = new List<AggregatedTimeSerie>();
        foreach (var aggregation in aggregations.Series)
        {
            aggregatedTimeSeries.Add(new AggregatedTimeSerie(
                MapPoints(aggregation.TimeSeriesPoints),
                MapMeteringPointType(aggregation.TimeSeriesType),
                MapUnitType(aggregation.QuantityUnit),
                MapResolution(aggregation.Resolution),
                await MapGridAreaDetailsAsync(aggregation.GridArea, cancellationToken).ConfigureAwait(false)));
        }

        return new AggregatedTimeSerieRequestWasAccepted(
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

    private static string MapMeteringPointType(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
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

    private static IReadOnlyList<Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints)
        {
            points.Add(new Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.Point(pointPosition, Parse(point.Quantity), MapQuality(point.QuantityQuality), point.Time.ToString()));
            pointPosition++;
        }

        return points.AsReadOnly();
    }

    private static string MapResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Pt15M => Common.Resolution.QuarterHourly.Name,
            Resolution.Pt1H => Common.Resolution.Hourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapUnitType(QuantityUnit quantityUnit)
    {
        return quantityUnit switch
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

    private async Task<GridAreaDetails> MapGridAreaDetailsAsync(string gridAreaCode, CancellationToken cancellationToken)
    {
        var gridOperatorNumber = await _gridAreaRepository.GetGridOwnerForAsync(gridAreaCode, cancellationToken).ConfigureAwait(false);

        return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    }
}
