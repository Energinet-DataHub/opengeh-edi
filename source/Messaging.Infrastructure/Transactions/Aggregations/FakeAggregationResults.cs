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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Transactions.Aggregations;

namespace Messaging.Infrastructure.Transactions.Aggregations;

public class FakeAggregationResults : IAggregationResults
{
    private readonly List<AggregationResult> _results = new();

    public Task<AggregationResult> GetResultAsync(Guid resultId, string gridArea)
    {
        return Task.FromResult(_results.First(result =>
            result.Id.Equals(resultId) &&
            result.GridAreaCode.Equals(gridArea, StringComparison.OrdinalIgnoreCase)));
    }

    public void Add(Guid resultId, AggregationResultDto aggregationResultDto)
    {
        ArgumentNullException.ThrowIfNull(aggregationResultDto);
        var points = aggregationResultDto.Points.Select(point =>
            new Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point(
                point.Position,
                decimal.Parse(point.Quantity, NumberStyles.Number, CultureInfo.InvariantCulture),
                point.Quality,
                point.QuarterTime));
        var result = new AggregationResult(resultId, points.ToList(), aggregationResultDto.GridAreaCode, aggregationResultDto.MeteringPointType, aggregationResultDto.MeasureUnitType, aggregationResultDto.Resolution);
        _results.Add(result);
    }
}
