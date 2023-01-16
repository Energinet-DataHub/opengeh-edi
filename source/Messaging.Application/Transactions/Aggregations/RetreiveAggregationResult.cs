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
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.Aggregations;

public class RetrieveAggregationResultHandler : IRequestHandler<RetrieveAggregationResult, Unit>
{
    private readonly IAggregatedTimeSeriesResults _aggregationResults;
    private readonly IAggregatedTimeSeriesTransactions _transactions;

    public RetrieveAggregationResultHandler(IAggregatedTimeSeriesResults aggregationResults, IAggregatedTimeSeriesTransactions transactions)
    {
        _aggregationResults = aggregationResults;
        _transactions = transactions;
    }

    public async Task<Unit> Handle(RetrieveAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var aggregationResult = await _aggregationResults.GetResultAsync(request.ResultId).ConfigureAwait(false);
        var transaction = await _transactions.GetAsync(TransactionId.Create(request.TransactionId.ToString())).ConfigureAwait(false);
        if (transaction is null) throw TransactionNotFoundException.TransactionIdNotFound(request.Id.ToString());
        transaction.SendResult(aggregationResult);
        return Unit.Value;
    }
}

public class RetrieveAggregationResult : InternalCommand
{
    [JsonConstructor]
    public RetrieveAggregationResult(Guid id, Guid resultId, Guid transactionId)
        : base(id)
    {
    }

    public RetrieveAggregationResult(Guid resultId, Guid transactionId)
    {
        ResultId = resultId;
        TransactionId = transactionId;
    }

    public Guid ResultId { get; }

    public Guid TransactionId { get; }
}
