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
using System.Linq;
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.Transactions.Aggregations;

namespace IntegrationTests.TestDoubles;

public class AggregationResultsStub : IAggregationResults
{
    private readonly List<AggregationResult> _results = new();
    private readonly Dictionary<ActorNumber, AggregationResult> _resultsForActors = new();

    public Task<AggregationResult> ProductionResultForAsync(Guid resultId, string gridArea, Domain.Transactions.Aggregations.Period period)
    {
        return Task.FromResult(_results.First(result =>
            result.Id.Equals(resultId) &&
            result.GridArea.Code.Equals(gridArea, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<ReadOnlyCollection<ActorNumber>> EnergySuppliersWithHourlyConsumptionResultAsync(Guid resultId, string gridArea)
    {
        var actors = _resultsForActors
            .Where(result => result.Value.GridArea.Code.Equals(gridArea, StringComparison.OrdinalIgnoreCase) && result.Value.Id.Equals(resultId))
            .Select(result => result.Key)
            .ToList();

        return Task.FromResult(actors.AsReadOnly());
    }

    public Task<AggregationResult> NonProfiledConsumptionForAsync(Guid resultId, string gridArea, ActorNumber energySupplierNumber, Domain.Transactions.Aggregations.Period period)
    {
        return Task.FromResult(_resultsForActors[energySupplierNumber]);
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
