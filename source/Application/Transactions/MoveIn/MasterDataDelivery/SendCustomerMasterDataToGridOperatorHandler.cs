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
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.MasterData.MarketEvaluationPoints;
using Energinet.DataHub.EDI.Domain.Transactions.MoveIn;
using MediatR;

namespace Energinet.DataHub.EDI.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorHandler : IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorRepository _actorRepository;

    public SendCustomerMasterDataToGridOperatorHandler(
        IMoveInTransactionRepository transactionRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorRepository actorRepository)
    {
        _transactionRepository = transactionRepository;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorRepository = actorRepository;
    }

    public async Task<Unit> Handle(SendCustomerMasterDataToGridOperator request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = _transactionRepository.GetById(request.ProcessId)
                          ?? throw TransactionNotFoundException.ProcessIdNotFound(request.ProcessId.Id.ToString());

        var gridOperatorNumber =
            await GetGridOperatorNumberAsync(transaction.MarketEvaluationPointId, cancellationToken)
                .ConfigureAwait(false);

        transaction.SendCustomerMasterDataToGridOperator(gridOperatorNumber);

        return Unit.Value;
    }

    private async Task<ActorNumber> GetGridOperatorNumberAsync(
        string marketEvaluationPointNumber,
        CancellationToken cancellationToken)
    {
        var marketEvaluationPoint = await _marketEvaluationPointRepository
            .GetByNumberAsync(marketEvaluationPointNumber).ConfigureAwait(false);
        return ActorNumber.Create(await _actorRepository
            .GetActorNumberByIdAsync(marketEvaluationPoint!.GridOperatorId.GetValueOrDefault(), cancellationToken)
            .ConfigureAwait(false));
    }
}
