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
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;

namespace Messaging.Application.Transactions.Aggregations;

public class SendAggregatedTimeSeriesHandler : IRequestHandler<SendAggregatedTimeSeries, Unit>
{
    private readonly IAggregatedTimeSeriesTransactions _transactions;
    private readonly IAggregatedTimeSeriesResults _aggregatedTimeSeriesResults;
    private readonly IGridAreaLookup _gridAreaLookup;

    public SendAggregatedTimeSeriesHandler(IAggregatedTimeSeriesTransactions transactions, IAggregatedTimeSeriesResults aggregatedTimeSeriesResults, IGridAreaLookup gridAreaLookup)
    {
        _transactions = transactions;
        _aggregatedTimeSeriesResults = aggregatedTimeSeriesResults;
        _gridAreaLookup = gridAreaLookup;
    }

    public async Task<Unit> Handle(SendAggregatedTimeSeries request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aggregatedTimeSeriesResult = await _aggregatedTimeSeriesResults.GetResultAsync(request.AggregatedTimeSeriesResultId).ConfigureAwait(false);
        foreach (var result in aggregatedTimeSeriesResult.Series)
        {
            var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(result.GridAreaCode).ConfigureAwait(false);
            var transaction = new AggregationResultForwarding(
                TransactionId.New(),
                gridOperatorNumber,
                MarketRole.GridOperator,
                ProcessType.BalanceFixing);
            _transactions.Add(transaction);
        }

        return Unit.Value;
    }
}

public class SendAggregatedTimeSeries : ICommand<Unit>
{
    [JsonConstructor]
    public SendAggregatedTimeSeries(Guid id, Guid aggregatedTimeSeriesResultId)
    {
    }

    public SendAggregatedTimeSeries(Guid aggregatedTimeSeriesResultId)
    {
        AggregatedTimeSeriesResultId = aggregatedTimeSeriesResultId;
    }

    public Guid AggregatedTimeSeriesResultId { get; }
}
