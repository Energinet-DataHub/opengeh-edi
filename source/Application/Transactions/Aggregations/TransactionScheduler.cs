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
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Application.Configuration.Commands;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;

namespace Application.Transactions.Aggregations;

public sealed class TransactionScheduler
{
    private readonly IAggregationResults _aggregationResults;
    private readonly ICommandScheduler _commandScheduler;

    public TransactionScheduler(IAggregationResults aggregationResults, ICommandScheduler commandScheduler)
    {
        _aggregationResults = aggregationResults;
        _commandScheduler = commandScheduler;
    }

    public async Task ScheduleForAsync(Guid resultsId, ProcessType aggregationProcess, GridArea gridArea, Domain.Transactions.Aggregations.Period period)
    {
        ArgumentNullException.ThrowIfNull(aggregationProcess);

        var results =
            await _aggregationResults.NonProfiledConsumptionForAsync(resultsId, gridArea, MarketRole.BalanceResponsible, period)
                .ConfigureAwait(false);

        await ScheduleTransactionsForAsync(aggregationProcess, results).ConfigureAwait(false);
    }

    private async Task ScheduleTransactionsForAsync(ProcessType aggregationProcess, ReadOnlyCollection<Result> results)
    {
        foreach (var result in results)
        {
            foreach (var aggregationResult in result.AggregationResults)
            {
                await _commandScheduler.EnqueueAsync(
                    new SendAggregationResult(
                        result.ReceiverNumber.Value,
                        MarketRole.BalanceResponsible.Name,
                        aggregationProcess.Name,
                        aggregationResult)).ConfigureAwait(false);
            }
        }
    }
}
