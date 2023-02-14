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
using Domain.Transactions.Aggregations;

namespace Application.Transactions.Aggregations;

public sealed class TransactionScheduler
{
    private readonly IAggregationResults _aggregationResults;
    private readonly ICommandScheduler _commandScheduler;
    private readonly IGridAreaLookup _gridAreaLookup;

    public TransactionScheduler(IAggregationResults aggregationResults, ICommandScheduler commandScheduler, IGridAreaLookup gridAreaLookup)
    {
        _aggregationResults = aggregationResults;
        _commandScheduler = commandScheduler;
        _gridAreaLookup = gridAreaLookup;
    }

    public async Task ScheduleForAsync(Guid resultsId, ProcessType aggregationProcess, GridArea gridArea, Domain.Transactions.Aggregations.Period period)
    {
        ArgumentNullException.ThrowIfNull(gridArea);
        ArgumentNullException.ThrowIfNull(aggregationProcess);

        await ScheduleTotalProductionResultAsync(resultsId, aggregationProcess, gridArea, period).ConfigureAwait(false);
        await ScheduleNonProfiledConsumptionForBalanceResponsibleAsync(resultsId, aggregationProcess, gridArea, period).ConfigureAwait(false);
        await ScheduleNonProfiledConsumptionForEnergySupplierAsync(resultsId, aggregationProcess, gridArea, period).ConfigureAwait(false);
    }

    private async Task ScheduleNonProfiledConsumptionForBalanceResponsibleAsync(
        Guid resultsId, ProcessType aggregationProcess, GridArea gridArea, Domain.Transactions.Aggregations.Period period)
    {
        await ScheduleTransactionsForAsync(aggregationProcess, await _aggregationResults.NonProfiledConsumptionForAsync(resultsId, gridArea, MarketRole.BalanceResponsible, period).ConfigureAwait(false)).ConfigureAwait(false);
    }

    private async Task ScheduleNonProfiledConsumptionForEnergySupplierAsync(
        Guid resultsId, ProcessType aggregationProcess, GridArea gridArea, Domain.Transactions.Aggregations.Period period)
    {
        var energySuppliers = await _aggregationResults
            .EnergySuppliersWithHourlyConsumptionResultAsync(resultsId, gridArea.Code).ConfigureAwait(false);
        foreach (var actorNumber in energySuppliers)
        {
            var result = await _aggregationResults.NonProfiledConsumptionForAsync(resultsId, gridArea.Code, actorNumber, period)
                .ConfigureAwait(false);
            await ScheduleAsync(aggregationProcess, actorNumber, MarketRole.EnergySupplier, result).ConfigureAwait(false);
        }
    }

    private async Task ScheduleTotalProductionResultAsync(Guid resultsId, ProcessType aggregationProcess, GridArea gridArea, Domain.Transactions.Aggregations.Period period)
    {
        var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(gridArea.Code).ConfigureAwait(false);
        var result = await _aggregationResults.ProductionResultForAsync(resultsId, gridArea.Code, period).ConfigureAwait(false);
        if (result is not null)
        {
            await _commandScheduler
                .EnqueueAsync(new SendAggregationResult(
                    gridOperatorNumber.Value,
                    MarketRole.MeteredDataResponsible.Name,
                    aggregationProcess.Name,
                    result)).ConfigureAwait(false);
        }
    }

    private async Task ScheduleTransactionsForAsync(ProcessType aggregationProcess, ReadOnlyCollection<Result> results)
    {
        foreach (var result in results)
        {
            foreach (var aggregationResult in result.AggregationResults)
            {
                await ScheduleAsync(aggregationProcess, result.ReceiverNumber, MarketRole.BalanceResponsible, aggregationResult)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task ScheduleAsync(ProcessType aggregationProcess, ActorNumber receivingActorNumber, MarketRole roleOfReceiver, AggregationResult result)
    {
        await _commandScheduler.EnqueueAsync(
            new SendAggregationResult(
                receivingActorNumber.Value,
                roleOfReceiver.Name,
                aggregationProcess.Name,
                result)).ConfigureAwait(false);
    }
}
