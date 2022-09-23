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
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Domain.Transactions.MoveIn.Events;

namespace Messaging.Application.Transactions.MoveIn.Notifications;

public class NotifyGridOperatorWhenConsumerHasMovedIn : INotificationHandler<BusinessProcessWasCompleted>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorLookup _actorLookup;
    private readonly MoveInNotifications _notifications;

    public NotifyGridOperatorWhenConsumerHasMovedIn(
        IMoveInTransactionRepository transactionRepository,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorLookup actorLookup,
        MoveInNotifications notifications)
    {
        _transactionRepository = transactionRepository;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorLookup = actorLookup;
        _notifications = notifications;
    }

    public async Task Handle(BusinessProcessWasCompleted notification, CancellationToken cancellationToken)
    {
        if (notification == null) throw new ArgumentNullException(nameof(notification));
        var transaction = _transactionRepository.GetById(notification.TransactionId);
        if (transaction is null)
        {
            throw TransactionNotFoundException.TransactionIdNotFound(notification.TransactionId);
        }

        var gridOperatorNumber = await GetGridOperatorNumberAsync(transaction.MarketEvaluationPointId).ConfigureAwait(false);
        _notifications.NotifyGridOperator(transaction, gridOperatorNumber);
    }

    private async Task<string> GetGridOperatorNumberAsync(string marketEvaluationPointNumber)
    {
        var marketEvaluationPoint =
            await _marketEvaluationPointRepository.GetByNumberAsync(marketEvaluationPointNumber)
                .ConfigureAwait(false);

        if (marketEvaluationPoint is null)
            throw new MoveInException($"Could not find market evaluation point with number {marketEvaluationPointNumber}");

        return await _actorLookup
            .GetActorNumberByIdAsync(marketEvaluationPoint.GridOperatorId.GetValueOrDefault())
            .ConfigureAwait(false);
    }
}
