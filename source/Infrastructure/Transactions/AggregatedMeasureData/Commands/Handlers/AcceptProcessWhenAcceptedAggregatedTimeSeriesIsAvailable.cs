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
using System.Threading;
using System.Threading.Tasks;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Infrastructure.Transactions.Aggregations;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;

public class AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable : IRequestHandler<AcceptedAggregatedTimeSeries, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Unit> Handle(AcceptedAggregatedTimeSeries request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _aggregatedMeasureDataProcessRepository
            .GetByIdAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);

        var aggregations = GetAggregations(request, process);

        process.IsAccepted(aggregations);

        return Unit.Value;
    }

    private static List<Aggregation> GetAggregations(AcceptedAggregatedTimeSeries request, AggregatedMeasureDataProcess process)
    {
        var aggregations = new List<Aggregation>();
        foreach (var aggregatedTimeSerie in request.AggregatedTimeSeries)
        {
            aggregations.Add(AggregationFactory.Create(process, aggregatedTimeSerie));
        }

        return aggregations;
    }
}
