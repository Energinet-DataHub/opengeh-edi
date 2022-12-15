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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Domain.Transactions.AggregatedTimeSeries;

namespace Messaging.Application.Transactions.AggregatedTimeSeries;

public class SendAggregatedTimeSeriesHandler : IRequestHandler<SendAggregatedTimeSeries, Unit>
{
    private readonly IAggregatedTimeSeriesTransactions _transactions;
    private readonly IAggregatedTimeSeriesResults _aggregatedTimeSeriesResults;

    public SendAggregatedTimeSeriesHandler(IAggregatedTimeSeriesTransactions transactions, IAggregatedTimeSeriesResults aggregatedTimeSeriesResults)
    {
        _transactions = transactions;
        _aggregatedTimeSeriesResults = aggregatedTimeSeriesResults;
    }

    public async Task<Unit> Handle(SendAggregatedTimeSeries request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aggregatedTimeSeriesResult = await _aggregatedTimeSeriesResults.GetResultAsync(request.AggregatedTimeSeriesResultId).ConfigureAwait(false);

        var transaction = new AggregatedTimeSeriesTransaction();
        transaction.SendResultToGridOperator(request.TimeSeries, ActorNumber.Create(request.GridOperatorNumber));
        _transactions.Add(transaction);
        return Unit.Value;
    }
}

public record SendAggregatedTimeSeries(TimeSeries TimeSeries, string GridOperatorNumber, Guid AggregatedTimeSeriesResultId) : ICommand<Unit>;
