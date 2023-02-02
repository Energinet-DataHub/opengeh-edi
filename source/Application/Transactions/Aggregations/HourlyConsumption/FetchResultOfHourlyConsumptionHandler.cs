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
using Application.Configuration.Commands;
using MediatR;

namespace Application.Transactions.Aggregations.HourlyConsumption;

public class FetchResultOfHourlyConsumptionHandler : IRequestHandler<FetchResultOfHourlyConsumption, Unit>
{
    private readonly IAggregationResults _aggregationResults;
    private readonly ICommandScheduler _commandScheduler;

    public FetchResultOfHourlyConsumptionHandler(IAggregationResults aggregationResults, ICommandScheduler commandScheduler)
    {
        _aggregationResults = aggregationResults;
        _commandScheduler = commandScheduler;
    }

    public async Task<Unit> Handle(FetchResultOfHourlyConsumption request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var energySupplierNumbers = await _aggregationResults
            .EnergySuppliersWithHourlyConsumptionResultAsync(request.ResultId, request.GridArea)
            .ConfigureAwait(false);

        var commandsToEnqueue = new List<Task>();
        foreach (var energySupplierNumber in energySupplierNumbers)
        {
            commandsToEnqueue.Add(_commandScheduler.EnqueueAsync(new StartTransaction()));
        }

        await Task.WhenAll(commandsToEnqueue).ConfigureAwait(false);
        return Unit.Value;
    }
}
