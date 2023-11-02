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
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.Edi.Responses;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Xunit;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class TestAggregatedTimeSeriesRequestAcceptedHandlerSpy : INotificationHandler<AggregatedTimeSerieRequestWasAccepted>
{
    private static readonly List<INotification> _actualNotifications = new();

    public static void AssertExpectedNotifications(AggregatedTimeSeriesRequestAccepted aggregatedTimeSeriesRequestAccepted)
    {
        if (aggregatedTimeSeriesRequestAccepted == null) throw new ArgumentNullException(nameof(aggregatedTimeSeriesRequestAccepted));

        Assert.NotNull(_actualNotifications);
        Assert.Single(_actualNotifications);
        Assert.Contains(_actualNotifications, notification => notification is AggregatedTimeSerieRequestWasAccepted);
        var actualNotification = _actualNotifications.Single() as AggregatedTimeSerieRequestWasAccepted;
        var actualTimeSerie = actualNotification!.AggregatedTimeSerie;
        Assert.Equal(aggregatedTimeSeriesRequestAccepted.GridArea, actualTimeSerie.GridAreaDetails.GridAreaCode);
        Assert.Equal(MapUnitType(aggregatedTimeSeriesRequestAccepted), actualTimeSerie.UnitType);
        Assert.Equal(MapMeteringPointType(aggregatedTimeSeriesRequestAccepted), actualTimeSerie.MeteringPointType);
        foreach (var point in actualTimeSerie.Points)
        {
            Assert.Contains(aggregatedTimeSeriesRequestAccepted.TimeSeriesPoints, exceptedPoint =>
                exceptedPoint.Time.ToString() == point.SampleTime &&
                MapQuality(exceptedPoint.QuantityQuality) == point.Quality &&
                Parse(exceptedPoint.Quantity) == point.Quantity);
        }
    }

    public Task Handle(AggregatedTimeSerieRequestWasAccepted notification, CancellationToken cancellationToken)
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

    private static string MapUnitType(AggregatedTimeSeriesRequestAccepted aggregation)
    {
        return aggregation.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }
}
