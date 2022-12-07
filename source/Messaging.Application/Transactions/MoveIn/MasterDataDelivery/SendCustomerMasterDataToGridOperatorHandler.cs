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
using Messaging.Application.Actors;
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.Actors;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.Transactions.MoveIn;

namespace Messaging.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorHandler : IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorLookup _actorLookup;

    public SendCustomerMasterDataToGridOperatorHandler(
        IMoveInTransactionRepository transactionRepository,
        IOutgoingMessageStore outgoingMessageStore,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorLookup actorLookup)
    {
        _transactionRepository = transactionRepository;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorLookup = actorLookup;
    }

    public async Task<Unit> Handle(SendCustomerMasterDataToGridOperator request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = _transactionRepository.GetById(request.TransactionId);
        if (transaction is null)
        {
            throw TransactionNotFoundException.TransactionIdNotFound(request.TransactionId);
        }

        var gridOperatorNumber =
            await GetGridOperatorNumberAsync(transaction.MarketEvaluationPointId)
                .ConfigureAwait(false);

        transaction.SendCustomerMasterDataToGridOperator(gridOperatorNumber);

        return Unit.Value;
    }

    private async Task<ActorNumber> GetGridOperatorNumberAsync(string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint = await _marketEvaluationPointRepository
            .GetByNumberAsync(marketEvaluationPointNumber).ConfigureAwait(false);
        return ActorNumber.Create(await _actorLookup.GetActorNumberByIdAsync(marketEvaluationPoint!.GridOperatorId.GetValueOrDefault())
            .ConfigureAwait(false));
    }
}
