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

using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

public class IncomingMessageReceiver : IIncomingMessageReceiver
{
    private readonly IncomingMessagePublisher _incomingMessagePublisher;
    private readonly IncomingMessagesContext _incomingMessagesContext;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly ITransactionIdRepository _transactionIdRepository;
    private readonly ILogger<IncomingMessageReceiver> _logger;

    public IncomingMessageReceiver(
        IncomingMessagePublisher incomingMessagePublisher,
        IncomingMessagesContext incomingMessagesContext,
        IMessageIdRepository messageIdRepository,
        ITransactionIdRepository transactionIdRepository,
        ILogger<IncomingMessageReceiver> logger)
    {
        _incomingMessagePublisher = incomingMessagePublisher;
        _incomingMessagesContext = incomingMessagesContext;
        _messageIdRepository = messageIdRepository;
        _transactionIdRepository = transactionIdRepository;
        _logger = logger;
    }

    public async Task<Result> ReceiveAsync(IIncomingMessage incomingMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessage);

        await AddMessageIdAndTransactionIdAsync(
                incomingMessage.SenderNumber,
                incomingMessage.MessageId,
                incomingMessage.Series.Select(x => x.TransactionId).ToList().AsReadOnly(),
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await ResilientTransaction.New(_incomingMessagesContext, async () =>
                {
                    await _incomingMessagePublisher.PublishAsync(
                            incomingMessage,
                            cancellationToken)
                        .ConfigureAwait(false);
                })
                .SaveChangesAsync(new DbContext[] { _incomingMessagesContext, })
                .ConfigureAwait(false);
            var transactionIds = string.Join(',', incomingMessage.Series.Select(x => x.TransactionId));
            _logger.LogInformation("Message with id {MessageId} received with transaction ids {TransactionIds}", incomingMessage.MessageId, transactionIds);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_TransactionRegistry_TransactionIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new DuplicateTransactionIdDetected());
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException
                                           && sqlException.Message.Contains(
                                               "Violation of PRIMARY KEY constraint 'PK_MessageRegistry_MessageIdAndSenderId'",
                                               StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(new DuplicateMessageIdDetected(incomingMessage.MessageId));
        }

        return Result.Succeeded();
    }

    private async Task AddMessageIdAndTransactionIdAsync(
        string senderNumber,
        string messageId,
        ReadOnlyCollection<string> transactionIds,
        CancellationToken cancellationToken)
    {
        await _transactionIdRepository.AddAsync(
            senderNumber,
            transactionIds,
            cancellationToken).ConfigureAwait(false);
        await _messageIdRepository.AddAsync(
                senderNumber,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
