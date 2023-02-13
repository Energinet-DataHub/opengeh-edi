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
using Application.Configuration.Commands.Commands;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using MediatR;

namespace Application.Transactions.Aggregations;

public class SendAggregationResult : InternalCommand
{
    [JsonConstructor]
    public SendAggregationResult(Guid id, ActorNumber sendResultTo, MarketRole roleOfReceiver, ProcessType processType, AggregationResult result)
    : base(id)
    {
        SendResultTo = sendResultTo;
        RoleOfReceiver = roleOfReceiver;
        ProcessType = processType;
        Result = result;
    }

    public SendAggregationResult(ActorNumber sendResultTo, MarketRole roleOfReceiver, ProcessType processType, AggregationResult result)
    {
        SendResultTo = sendResultTo;
        RoleOfReceiver = roleOfReceiver;
        ProcessType = processType;
        Result = result;
    }

    public ActorNumber SendResultTo { get; }

    public MarketRole RoleOfReceiver { get; }

    public ProcessType ProcessType { get; }

    public AggregationResult Result { get; }
}

public class SendAggregationResultHandler : IRequestHandler<SendAggregationResult, Unit>
{
    private readonly IAggregationResultForwardingRepository _repository;

    public SendAggregationResultHandler(IAggregationResultForwardingRepository repository)
    {
        _repository = repository;
    }

    public Task<Unit> Handle(SendAggregationResult request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = new AggregationResultForwarding(
            TransactionId.New(),
            request.SendResultTo,
            request.RoleOfReceiver,
            request.ProcessType);

        transaction.SendResult(request.Result);

        _repository.Add(transaction);
        return Task.FromResult(Unit.Value);
    }
}
