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
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands.Commands;
using Application.OutgoingMessages;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using MediatR;

namespace Application.Transactions.Aggregations;

public class ZForwardAggregationResult : InternalCommand
{
    [JsonConstructor]
    public ZForwardAggregationResult(Guid id, Aggregation result)
    : base(id)
    {
        Result = result;
    }

    public ZForwardAggregationResult(Aggregation result)
    {
        Result = result;
    }

    public Aggregation Result { get; }
}

public class ZForwardAggregationResultHandler : IRequestHandler<ZForwardAggregationResult, Unit>
{
    private readonly IGridAreaLookup _gridAreaLookup;
    private readonly IAggregationResultForwardingRepository _transactions;
    private readonly IOutgoingMessageStore _outgoingMessageStore;

    public ZForwardAggregationResultHandler(IGridAreaLookup gridAreaLookup, IAggregationResultForwardingRepository transactions, IOutgoingMessageStore outgoingMessageStore)
    {
        _gridAreaLookup = gridAreaLookup;
        _transactions = transactions;
        _outgoingMessageStore = outgoingMessageStore;
    }

    public async Task<Unit> Handle(ZForwardAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var gridOperator = await _gridAreaLookup.GetGridOperatorForAsync(request.Result.GridArea).ConfigureAwait(false);
        var transaction = new AggregationResultForwarding(TransactionId.New(), gridOperator, MarketRole.MeteredDataResponsible, EnumerationType.FromName<ProcessType>(request.Result.ProcessType));
        _transactions.Add(transaction);

        var outgoingMessage = AggregationResultMessage.Create(gridOperator, MarketRole.MeteredDataResponsible, transaction.Id, ProcessType.BalanceFixing, CreateFrom(transaction.Id, request.Result));
        _outgoingMessageStore.Add(outgoingMessage);

        return Unit.Value;
    }

    private static TimeSeries CreateFrom(TransactionId transactionId, Aggregation result)
    {
        return new TimeSeries(
            transactionId.Id,
            result.GridArea,
            result.MeteringPointType,
            result.SettlementType,
            result.MeasureUnitType,
            result.Resolution,
            null,
            null,
            new Domain.Transactions.Aggregations.Period(result.Period.Start, result.Period.End),
            result.Points.Select(point => new Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point(point.Position, point.Quantity, point.Quality, point.SampleTime)).ToList());
    }
}
