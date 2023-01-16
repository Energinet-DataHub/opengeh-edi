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
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;

namespace Messaging.Application.Transactions.Aggregations;

public class StartTransactionHandler : IRequestHandler<StartTransaction, Unit>
{
    private readonly IGridAreaLookup _gridAreaLookup;
    private readonly IAggregatedTimeSeriesTransactions _transactions;
    private readonly ICommandScheduler _commandScheduler;

    public StartTransactionHandler(IGridAreaLookup gridAreaLookup, IAggregatedTimeSeriesTransactions transactions, ICommandScheduler commandScheduler)
    {
        _gridAreaLookup = gridAreaLookup;
        _transactions = transactions;
        _commandScheduler = commandScheduler;
    }

    public async Task<Unit> Handle(StartTransaction request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(request.GridAreaCode).ConfigureAwait(false);
        var transaction = new AggregatedTimeSeriesTransaction(
            TransactionId.New(),
            gridOperatorNumber,
            MarketRole.GridOperator,
            ProcessType.BalanceFixing);

        _transactions.Add(transaction);
        await _commandScheduler.EnqueueAsync(new RetrieveAggregationResult(request.ResultId, Guid.Parse(transaction.Id.Id))).ConfigureAwait(false);

        return Unit.Value;
    }
}

public class StartTransaction : InternalCommand
{
    public StartTransaction(string gridAreaCode, Guid resultId)
    {
        GridAreaCode = gridAreaCode;
        ResultId = resultId;
    }

    [JsonConstructor]
    public StartTransaction(Guid id, string gridAreaCode, Guid resultId)
        : base(id)
    {
        GridAreaCode = gridAreaCode;
        ResultId = resultId;
    }

    public string GridAreaCode { get; }

    public Guid ResultId { get; }
}
