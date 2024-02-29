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

using System.Linq;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Xunit;
using Point = Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices.Point;
using PointOutgoing = Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Models;

public class TimeSeriesModelTests
{
    // TODO: consider testing the serialization of the messages instead
    [Fact]
    public void EnergyResultMessage_has_the_same_attributes_as_TimeSeriesMarketActivityRecord()
    {
        var pointAttributeName = "Point";
        var propertyInfosOfTimeSeries = typeof(EnergyResultMessageTimeSeries).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecord = typeof(TimeSeriesMarketActivityRecord).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Points are duplicated, hence we ignore them in the comparison of timeseries and timeseriesmarketactivityrecord
        var propertyInfosOfTimeSeriesWithoutPointAttribute = propertyInfosOfTimeSeries.Where(p => p.Name != pointAttributeName).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute = propertyInfosOfTimeSeriesMarketActivityRecord.Where(p => p.Name != pointAttributeName).ToList();

        // We have to compare the point attributes separately
        var pointOfTimeSeries = propertyInfosOfTimeSeries.Single(p => p.Name == pointAttributeName);
        var pointOfTimeSeriesTimeSeriesMarketActivityRecord = propertyInfosOfTimeSeriesMarketActivityRecord.Single(p => p.Name == pointAttributeName);
        var pointTypeOfTimeSeries = typeof(PointOutgoing).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var pointTypeOfTimeSeriesMarketActivityRecord = typeof(PointOutgoing).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Assert that the non-point attributes are the same
        Assert.All(propertyInfosOfTimeSeriesWithoutPointAttribute, property =>
            Assert.Contains(propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute, element =>
                element.Name == property.Name && element.PropertyType == property.PropertyType));

        // Assert that the point attributes are the same and that the points are not the same class
        Assert.NotEqual(pointOfTimeSeries.PropertyType, pointOfTimeSeriesTimeSeriesMarketActivityRecord.PropertyType);
        Assert.Equal(pointTypeOfTimeSeries, pointTypeOfTimeSeriesMarketActivityRecord);
    }

    [Fact]
    public void WholesaleMessage_TimeSeries_has_the_same_attributes_as_TimeSeriesMarketActivityRecord()
    {
        var pointAttributeName = "Points";
        var propertyInfosOfTimeSeries = typeof(WholesaleCalculationSeries).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecord = typeof(WholesaleCalculationMarketActivityRecord).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Points are duplicated, hence we ignore them in the comparison of timeseries and timeseriesmarketactivityrecord
        var propertyInfosOfTimeSeriesWithoutPointAttribute = propertyInfosOfTimeSeries.Where(p => p.Name != pointAttributeName).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute = propertyInfosOfTimeSeriesMarketActivityRecord.Where(p => p.Name != pointAttributeName).ToList();

        // We have to compare the point attributes separately
        var pointOfTimeSeries = propertyInfosOfTimeSeries.Single(p => p.Name == pointAttributeName);
        var pointOfTimeSeriesTimeSeriesMarketActivityRecord = propertyInfosOfTimeSeriesMarketActivityRecord.Single(p => p.Name == pointAttributeName);
        var pointTypeOfTimeSeries = typeof(WholesaleCalculationPoint).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var pointTypeOfTimeSeriesMarketActivityRecord = typeof(Point).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Assert that the non-point attributes are the same
        Assert.All(propertyInfosOfTimeSeriesWithoutPointAttribute, property =>
            Assert.Contains(propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute, element =>
                element.Name == property.Name && element.PropertyType == property.PropertyType));

        // Assert that the point attributes are the same and that the points are not the same class
        Assert.NotEqual(pointOfTimeSeries.PropertyType, pointOfTimeSeriesTimeSeriesMarketActivityRecord.PropertyType);
        Assert.Equal(pointTypeOfTimeSeries, pointTypeOfTimeSeriesMarketActivityRecord);
    }

    [Fact]
    public void AcceptedEnergyResultMessage_TimeSeries_has_the_same_attributes_as_TimeSeriesMarketActivityRecord()
    {
        var pointAttributeName = "Point";
        var propertyInfosOfTimeSeries = typeof(AcceptedEnergyResultMessageTimeSeries).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecord = typeof(TimeSeriesMarketActivityRecord).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Points are duplicated, hence we ignore them in the comparison of timeseries and timeseriesmarketactivityrecord
        var propertyInfosOfTimeSeriesWithoutPointAttribute = propertyInfosOfTimeSeries.Where(p => p.Name != pointAttributeName).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute = propertyInfosOfTimeSeriesMarketActivityRecord.Where(p => p.Name != pointAttributeName).ToList();

        // We have to compare the point attributes separately
        var pointOfTimeSeries = propertyInfosOfTimeSeries.Single(p => p.Name == pointAttributeName);
        var pointOfTimeSeriesTimeSeriesMarketActivityRecord = propertyInfosOfTimeSeriesMarketActivityRecord.Single(p => p.Name == pointAttributeName);
        var pointTypeOfTimeSeries = typeof(AcceptedEnergyResultMessagePoint).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var pointTypeOfTimeSeriesMarketActivityRecord = typeof(PointOutgoing).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Assert that the non-point attributes are the same
        Assert.All(propertyInfosOfTimeSeriesWithoutPointAttribute, property =>
            Assert.Contains(propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute, element =>
                element.Name == property.Name && element.PropertyType == property.PropertyType));

        // Assert that the point attributes are the same and that the points are not the same class
        Assert.NotEqual(pointOfTimeSeries.PropertyType, pointOfTimeSeriesTimeSeriesMarketActivityRecord.PropertyType);
        Assert.Equal(pointTypeOfTimeSeries, pointTypeOfTimeSeriesMarketActivityRecord);
    }
}
