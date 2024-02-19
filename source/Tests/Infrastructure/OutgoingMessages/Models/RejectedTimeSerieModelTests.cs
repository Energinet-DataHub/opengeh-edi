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
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.OutgoingMessages;
using Xunit;
using RejectReasonOutgoing = Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData.RejectReason;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Models;

public class RejectedTimeSerieModelTests
{
    [Fact]
    public void RejectedTimeSerie_has_the_same_attributes_as_RejectedTimeSerieMarketActivityRecord()
    {
        var rejectReasonsAttributeName = "RejectReasons";
        var propertyInfOfRejectedTimeSerie = typeof(RejectedTimeSerie).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var propertyInfOfRejectedTimeSerieMarketActivityRecord = typeof(RejectedTimeSerieMarketActivityRecord).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // RejectReasons are duplicated, hence we ignore them in the comparison of rejectedtimeserie and rejectedtimeseremarketactivityrecord
        var propertyInfosOfTimeSeriesWithoutPointAttribute = propertyInfOfRejectedTimeSerie.Where(p => p.Name != rejectReasonsAttributeName).ToList();
        var propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute = propertyInfOfRejectedTimeSerieMarketActivityRecord.Where(p => p.Name != rejectReasonsAttributeName).ToList();

        // We compare reject reasons separately
        var rejectReasonsOfRejectedTimeSerie = propertyInfOfRejectedTimeSerie.Single(p => p.Name == rejectReasonsAttributeName);
        var rejectReasonsOfRejectedTimeSerieMarketActivityRecord = propertyInfOfRejectedTimeSerieMarketActivityRecord.Single(p => p.Name == rejectReasonsAttributeName);
        var rejectReasonOfRejectedTimeSerie = typeof(Process.Domain.Transactions.AggregatedMeasureData.RejectReason).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();
        var rejectReasonOfRejectedTimeSerieMarketActivityRecord = typeof(RejectReasonOutgoing).GetProperties().Select(p => new { Name = p.Name, PropertyType = p.PropertyType.ToString() }).ToList();

        // Assert that the non-reject reason attributes are the same
        Assert.All(propertyInfosOfTimeSeriesWithoutPointAttribute, property =>
            Assert.Contains(propertyInfosOfTimeSeriesMarketActivityRecordWithoutPointAttribute, element =>
                element.Name == property.Name && element.PropertyType == property.PropertyType));

        // Assert that the reject reason attributes are the same and that the reject reasons are not the same class
        Assert.NotEqual(rejectReasonsOfRejectedTimeSerie.PropertyType, rejectReasonsOfRejectedTimeSerieMarketActivityRecord.PropertyType);
        Assert.Equal(rejectReasonOfRejectedTimeSerie, rejectReasonOfRejectedTimeSerieMarketActivityRecord);
    }
}
