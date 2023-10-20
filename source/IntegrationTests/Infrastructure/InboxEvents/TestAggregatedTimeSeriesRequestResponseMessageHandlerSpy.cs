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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.Edi.Responses;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class TestAggregatedTimeSeriesRequestResponseMessageHandlerSpy : INotificationHandler<AggregatedTimeSerieRequestResponse>
{
    private static readonly List<INotification> _actualNotifications = new();

    public static void AssertExpectedNotifications(AggregatedTimeSeriesRequestResponseMessage aggregatedTimeSeriesRequestAccepted)
    {
        if (aggregatedTimeSeriesRequestAccepted == null) throw new ArgumentNullException(nameof(aggregatedTimeSeriesRequestAccepted));

        Assert.NotNull(_actualNotifications);
        Assert.Single(_actualNotifications);
        Assert.Contains(_actualNotifications, notification => notification is AggregatedTimeSerieRequestResponse);
        var actualNotification = _actualNotifications.Single() as AggregatedTimeSerieRequestResponse;
        var actualTimeSerie = actualNotification!.AggregatedTimeSerie;
        Assert.Equal(aggregatedTimeSeriesRequestAccepted.GridArea, actualTimeSerie.GridAreaDetails.GridAreaCode);
        Assert.Equal(aggregatedTimeSeriesRequestAccepted.SettlementVersion, actualTimeSerie.SettlementVersion);
        Assert.Equal(aggregatedTimeSeriesRequestAccepted.Period.StartOfPeriod.ToInstant(), actualTimeSerie.Period.Start);
        Assert.Equal(aggregatedTimeSeriesRequestAccepted.Period.EndOfPeriod.ToInstant(), actualTimeSerie.Period.End);
        Assert.Equal(MapUnitType(aggregatedTimeSeriesRequestAccepted), actualTimeSerie.UnitType);
        Assert.Equal(MapResolution(aggregatedTimeSeriesRequestAccepted.Period.Resolution), actualTimeSerie.Resolution);
        Assert.Equal(MapMeteringPointType(aggregatedTimeSeriesRequestAccepted), actualTimeSerie.MeteringPointType);
        foreach (var point in actualTimeSerie.Points)
        {
            Assert.Contains(aggregatedTimeSeriesRequestAccepted.TimeSeriesPoints, exceptedPoint =>
                exceptedPoint.Time.ToString() == point.SampleTime &&
                MapQuality(exceptedPoint.QuantityQuality) == point.Quality &&
                Parse(exceptedPoint.Quantity) == point.Quantity);
        }
    }

    public Task Handle(AggregatedTimeSerieRequestResponse notification, CancellationToken cancellationToken)
    {
        _actualNotifications.Add(notification);
        return Task.CompletedTask;
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

    private static string MapMeteringPointType(AggregatedTimeSeriesRequestResponseMessage aggregation)
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

    private static string MapUnitType(AggregatedTimeSeriesRequestResponseMessage aggregation)
    {
        return aggregation.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }
}
