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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Equivalency;
using MediatR;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class TestAggregatedTimeSeriesRequestAcceptedHandlerSpy : INotificationHandler<AggregatedTimeSerieRequestWasAccepted>
{
    private static readonly List<INotification> _actualNotifications = new();

    public static void AssertExpectedNotifications(AggregatedTimeSeriesRequestAccepted aggregatedTimeSeriesRequestAccepted)
    {
        ArgumentNullException.ThrowIfNull(aggregatedTimeSeriesRequestAccepted);

        var firstSeries = aggregatedTimeSeriesRequestAccepted.Series.FirstOrDefault();
        firstSeries.Should().NotBeNull();

        _actualNotifications.Should().NotBeNull();
        _actualNotifications.Should().ContainSingle();
        _actualNotifications.Should().ContainItemsAssignableTo<AggregatedTimeSerieRequestWasAccepted>();

        var actualNotification = _actualNotifications.Single() as AggregatedTimeSerieRequestWasAccepted;
        actualNotification.Should().NotBeNull();
        actualNotification!.AggregatedTimeSeries.Should().ContainSingle();

        var actualTimeSeries = actualNotification.AggregatedTimeSeries[0];
        actualTimeSeries.Should().NotBeNull();
        actualTimeSeries.GridAreaDetails.GridAreaCode.Should().Be(firstSeries!.GridArea);
        actualTimeSeries.UnitType.Should().Be(MapUnitType(firstSeries));
        actualTimeSeries.MeteringPointType.Should().Be(MapMeteringPointType(firstSeries));
        actualTimeSeries.Points.Should()
            .BeEquivalentTo(
                firstSeries.TimeSeriesPoints,
                opt => opt.Using(new PointsComparer()));
    }

    public Task Handle(AggregatedTimeSerieRequestWasAccepted notification, CancellationToken cancellationToken)
    {
        _actualNotifications.Add(notification);
        return Task.CompletedTask;
    }

    private static CalculatedQuantityQuality MapQuality(QuantityQuality quality)
    {
        return quality switch
        {
            QuantityQuality.Measured => CalculatedQuantityQuality.Measured,
            QuantityQuality.Missing => CalculatedQuantityQuality.Missing,
            QuantityQuality.Estimated => CalculatedQuantityQuality.Estimated,
            QuantityQuality.Calculated => CalculatedQuantityQuality.Calculated,
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

    private static string MapMeteringPointType(Series aggregation)
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

    private static string MapUnitType(Series aggregation)
    {
        return aggregation.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private sealed class PointsComparer : IEquivalencyStep
    {
        public EquivalencyResult Handle(
            Comparands comparands,
            IEquivalencyValidationContext context,
            IEquivalencyValidator nestedValidator)
        {
            if (comparands is not { Subject: Point p, Expectation: TimeSeriesPoint tsp })
            {
                return EquivalencyResult.ContinueWithNext;
            }

            tsp.QuantityQualities.Should()
                .ContainSingle("this is just a migration of an old test, where we only had one quality");

            p.SampleTime.Should().Be(tsp.Time.ToString());
            p.QuantityQuality.Should().Be(MapQuality(tsp.QuantityQualities.Single()));
            p.Quantity.Should().Be(Parse(tsp.Quantity));

            return EquivalencyResult.AssertionCompleted;
        }
    }
}
