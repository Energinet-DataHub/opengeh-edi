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
using System.Linq;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations;
using Xunit;
using Point = Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices.Point;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Models;

public class WholesaleCalculationModelTests
{
    [Fact]
    public void WholesaleCalculationSeries_has_the_same_attributes_as_WholesaleCalculationMarketActivityRecord()
    {
        var pointsAttributeName = "Points";
        var keysOfWholesaleCalculationSeries = typeof(WholesaleCalculationSeries).GetProperties()
            .Select(p => new { Name = p.Name, PropertyType = p.PropertyType })
            .Where(p => p.Name != pointsAttributeName).ToList();
        var keysOfWholesaleCalculationMarketActivityRecord = typeof(NotifyWholesaleServicesMarketActivityRecord).GetProperties()
            .Select(p => new { Name = p.Name, PropertyType = p.PropertyType })
            .Where(p => p.Name != pointsAttributeName).ToList();

        Assert.All(keysOfWholesaleCalculationSeries, property =>
            Assert.Contains(keysOfWholesaleCalculationMarketActivityRecord, element =>
                element.Name == property.Name
                && element.PropertyType == property.PropertyType
                && Nullable.GetUnderlyingType(element.PropertyType) == Nullable.GetUnderlyingType(property.PropertyType)));
    }

    [Fact]
    public void WholesaleCalculationSeries_point_has_the_same_attributes_as_WholesaleCalculationMarketActivityRecord_point()
    {
        var keysOfWholesaleCalculationSeries = typeof(Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleCalculations.Point).GetProperties()
            .Select(p => new { Name = p.Name, PropertyType = p.PropertyType }).ToList();
        var keysOfWholesaleCalculationMarketActivityRecord = typeof(Point).GetProperties()
            .Select(p => new { Name = p.Name, PropertyType = p.PropertyType }).ToList();

        Assert.All(keysOfWholesaleCalculationSeries, property =>
            Assert.Contains(keysOfWholesaleCalculationMarketActivityRecord, element =>
                element.Name == property.Name
                && element.PropertyType == property.PropertyType
                && Nullable.GetUnderlyingType(element.PropertyType) == Nullable.GetUnderlyingType(property.PropertyType)));
    }
}
