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
using Messaging.Application.Common;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.GenericNotification;
using Processing.Domain.SeedWork;

namespace Messaging.Application.Transactions.MoveIn;

public class CompleteMoveInTransactionHandler : IRequestHandler<CompleteMoveInTransaction, Unit>
{
    private readonly IMoveInTransactionRepository _transactionRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteMoveInTransactionHandler(IMoveInTransactionRepository transactionRepository, ISystemDateTimeProvider systemDateTimeProvider, IMarketActivityRecordParser marketActivityRecordParser, IOutgoingMessageStore outgoingMessageStore, IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
        _marketActivityRecordParser = marketActivityRecordParser;
        _outgoingMessageStore = outgoingMessageStore;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CompleteMoveInTransaction request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var transaction = await _transactionRepository.GetByProcessIdAsync(request.ProcessId).ConfigureAwait(false);
        if (transaction is null)
        {
            throw new TransactionNotFoundException(request.ProcessId);
        }

        var header = new MessageHeader(
        "E01",
        "senderid",
        "senderrole",
        "receiverid",
        "receiverrole",
        Guid.NewGuid().ToString(),
        _systemDateTimeProvider.Now());
        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            transaction.TransactionId,
            transaction.MarketEvaluationPointId,
            transaction.EffectiveDate);

        var message = new OutgoingMessage(
            "GenericNotification",
            header.ReceiverId,
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            header.ProcessType,
            header.ReceiverRole,
            header.SenderId,
            header.SenderRole,
            _marketActivityRecordParser.From(marketActivityRecord),
            null);

        _outgoingMessageStore.Add(message);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        return Unit.Value;
    }
}
