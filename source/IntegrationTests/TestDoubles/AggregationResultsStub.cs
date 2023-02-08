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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Domain.Transactions.Aggregations;
using Infrastructure.Transactions.Aggregations;
using Period = Domain.Transactions.Aggregations.Period;

namespace IntegrationTests.TestDoubles;

public class AggregationResultsStub : IAggregationResults
{
    private readonly List<AggregationResult> _results = new();
    private readonly Dictionary<ActorNumber, AggregationResult> _resultsForActors = new();

    public Task<AggregationResult> ProductionResultForAsync(Guid resultId, string gridArea, Domain.Transactions.Aggregations.Period period)
    {
        return Task.FromResult(_results.First(result =>
            result.Id.Equals(resultId) &&
            result.GridAreaCode.Equals(gridArea, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<ReadOnlyCollection<ActorNumber>> EnergySuppliersWithHourlyConsumptionResultAsync(Guid resultId, string gridArea)
    {
        var actors = _resultsForActors
            .Where(result => result.Value.GridAreaCode.Equals(gridArea, StringComparison.OrdinalIgnoreCase) && result.Value.Id.Equals(resultId))
            .Select(result => result.Key)
            .ToList();

        return Task.FromResult(actors.AsReadOnly());
    }

    public Task<AggregationResult> NonProfiledConsumptionForAsync(Guid resultId, string gridArea, ActorNumber energySupplierNumber, Domain.Transactions.Aggregations.Period period)
    {
        return Task.FromResult(_resultsForActors[energySupplierNumber]);
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
        var result = new AggregationResult(
            resultId,
            points.ToList(),
            aggregationResultDto.GridAreaCode,
            EnumerationType.FromName<MeteringPointType>(aggregationResultDto.MeteringPointType),
            aggregationResultDto.MeasureUnitType,
            aggregationResultDto.Resolution,
            new Period(aggregationResultDto.PeriodStart, aggregationResultDto.PeriodEnd));
        _results.Add(result);
    }

    public void Add(AggregationResult aggregationResult, ActorNumber targetActorNumber)
    {
        ArgumentNullException.ThrowIfNull(aggregationResult);
        _resultsForActors.Add(targetActorNumber, aggregationResult);
    }

    public void Add(AggregationResult aggregationResult)
    {
        ArgumentNullException.ThrowIfNull(aggregationResult);
        _results.Add(aggregationResult);
    }
}
