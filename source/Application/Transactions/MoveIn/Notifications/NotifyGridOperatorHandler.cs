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
using Application.Actors;
using Domain.MasterData.MarketEvaluationPoints;
using Domain.Transactions;
using Domain.Transactions.MoveIn;
using MediatR;

namespace Application.Transactions.MoveIn.Notifications;

public class NotifyGridOperatorHandler : IRequestHandler<NotifyGridOperator, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorRepository _actorRepository;
    private readonly MoveInNotifications _notifications;

    public NotifyGridOperatorHandler(
        IMoveInTransactionRepository transactionRepository,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorRepository actorRepository,
        MoveInNotifications notifications)
    {
        _transactionRepository = transactionRepository;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorRepository = actorRepository;
        _notifications = notifications;
    }

    public async Task<Unit> Handle(NotifyGridOperator request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transaction = _transactionRepository.GetById(request.ProcessId)
                          ?? throw TransactionNotFoundException.ProcessIdNotFound(request.ProcessId.Id.ToString());

        var gridOperatorNumber = await GetGridOperatorNumberAsync(transaction.MarketEvaluationPointId, cancellationToken).ConfigureAwait(false);
        _notifications.NotifyGridOperator(transaction, gridOperatorNumber);
        transaction.SetGridOperatorWasNotified();

        return Unit.Value;
    }

    private async Task<string> GetGridOperatorNumberAsync(string marketEvaluationPointNumber, CancellationToken cancellationToken)
    {
        var marketEvaluationPoint =
            await _marketEvaluationPointRepository.GetByNumberAsync(marketEvaluationPointNumber)
                .ConfigureAwait(false) ?? throw new MoveInException($"Could not find market evaluation point with number {marketEvaluationPointNumber}");

        return await _actorRepository
            .GetActorNumberByIdAsync(marketEvaluationPoint.GridOperatorId.GetValueOrDefault(), cancellationToken)
            .ConfigureAwait(false);
    }
}
