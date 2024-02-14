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

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Models;

public class RejectedTimeSerieModelTests
{
    [Fact]
    public void RejectedTimeSerie_has_the_same_attributes_as_RejectedTimeSerieMarketActivityRecord()
    {
        var keysOfRejectedTimeSerie = typeof(RejectedTimeSerie).GetProperties().Select(p => new PropertyInfo(p.Name, p.PropertyType.ToString())).ToList();
        var keysOfRejectedTimeSerieMarketActivityRecord = typeof(RejectedTimeSerieMarketActivityRecord).GetProperties().Select(p => new PropertyInfo(p.Name, p.PropertyType.ToString())).ToList();

        Assert.All(keysOfRejectedTimeSerie, property =>
            Assert.Contains(keysOfRejectedTimeSerieMarketActivityRecord, element =>
                element.Name == property.Name && element.PropertyType == property.PropertyType));
    }

    private sealed record PropertyInfo(string Name, string PropertyType);
}
