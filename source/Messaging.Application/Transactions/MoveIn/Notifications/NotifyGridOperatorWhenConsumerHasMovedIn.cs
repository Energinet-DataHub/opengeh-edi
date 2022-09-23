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
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Domain.Transactions.MoveIn.Events;

namespace Messaging.Application.Transactions.MoveIn.Notifications;

public class NotifyGridOperatorWhenConsumerHasMovedIn : INotificationHandler<BusinessProcessWasCompleted>
{
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private readonly IMarketEvaluationPointRepository _marketEvaluationPointRepository;
    private readonly IActorLookup _actorLookup;

    public NotifyGridOperatorWhenConsumerHasMovedIn(
        IOutgoingMessageStore outgoingMessageStore,
        IMoveInTransactionRepository transactionRepository,
        IMarketActivityRecordParser marketActivityRecordParser,
        IMarketEvaluationPointRepository marketEvaluationPointRepository,
        IActorLookup actorLookup)
    {
        _outgoingMessageStore = outgoingMessageStore;
        _transactionRepository = transactionRepository;
        _marketActivityRecordParser = marketActivityRecordParser;
        _marketEvaluationPointRepository = marketEvaluationPointRepository;
        _actorLookup = actorLookup;
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

        var marketActivityRecord = new OutgoingMessages.GenericNotification.MarketActivityRecord(
            Guid.NewGuid().ToString(),
            notification.TransactionId,
            transaction!.MarketEvaluationPointId,
            transaction.EffectiveDate);

        var message = new OutgoingMessage(
            DocumentType.GenericNotification,
            gridOperatorNumber,
            transaction.TransactionId,
            ProcessType.MoveIn.Code,
            MarketRoles.GridOperator,
            DataHubDetails.IdentificationNumber,
            MarketRoles.MeteringPointAdministrator,
            _marketActivityRecordParser.From(marketActivityRecord));

        _outgoingMessageStore.Add(message);
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
