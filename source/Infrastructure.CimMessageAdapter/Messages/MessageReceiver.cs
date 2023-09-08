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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.IncomingMessages;
using CimMessageAdapter.Messages.Queues;
using CimMessageAdapter.ValidationErrors;
using MessageHeader = Application.IncomingMessages.MessageHeader;

namespace CimMessageAdapter.Messages
{
    public abstract class MessageReceiver<TQueue>
        where TQueue : Queue
    {
        private const int MessageIdLength = 36;
        private const int TransactionIdLength = 36;
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly IMessageQueueDispatcher<TQueue> _messageQueueDispatcher;
        private readonly ITransactionIds _transactionIds;
        private readonly ISenderAuthorizer _senderAuthorizer;
        private readonly IProcessTypeValidator _processTypeValidator;
        private readonly IMessageTypeValidator _messageTypeValidator;
        private readonly IReceiverValidator _receiverValidator;

        protected MessageReceiver(
            IMessageIds messageIds,
            IMessageQueueDispatcher<TQueue> messageQueueDispatcher,
            ITransactionIds transactionIds,
            ISenderAuthorizer senderAuthorizer,
            IProcessTypeValidator processTypeValidator,
            IMessageTypeValidator messageTypeValidator,
            IReceiverValidator receiverValidator)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _messageQueueDispatcher = messageQueueDispatcher ??
                                             throw new ArgumentNullException(nameof(messageQueueDispatcher));
            _transactionIds = transactionIds;
            _senderAuthorizer = senderAuthorizer;
            _processTypeValidator = processTypeValidator;
            _messageTypeValidator = messageTypeValidator;
            _receiverValidator = receiverValidator;
        }

        public async Task<Result> ReceiveAsync<TMarketActivityRecordType, TMarketTransactionType>(
            MessageParserResult<TMarketActivityRecordType, TMarketTransactionType> messageParserResult,
            CancellationToken cancellationToken)
            where TMarketActivityRecordType : IMarketActivityRecord
            where TMarketTransactionType : IMarketTransaction<TMarketActivityRecordType>
        {
            ArgumentNullException.ThrowIfNull(messageParserResult);

            var messageHeader = messageParserResult.IncomingMarketDocument?.Header;
            var marketDocument = messageParserResult.IncomingMarketDocument;

            if (messageHeader is null)
            {
                return Result.Failure(messageParserResult.Errors.ToArray());
            }

            ArgumentNullException.ThrowIfNull(marketDocument);

            MessageIdIsEmpty(messageHeader.MessageId);

            var authorizeSenderTask = AuthorizeSenderAsync(messageHeader);
            var verifyReceiverTask = VerifyReceiverAsync(messageHeader);
            var checkMessageIdTask = CheckMessageIdAsync(messageHeader.SenderId, messageHeader.MessageId, cancellationToken);
            var checkMessageTypeTask = CheckMessageTypeAsync(messageHeader.MessageType, cancellationToken);
            var checkProcessTypeTask = CheckProcessTypeAsync(messageHeader.BusinessReason, cancellationToken);

            await Task.WhenAll(
                authorizeSenderTask,
                verifyReceiverTask,
                checkMessageIdTask,
                checkMessageTypeTask,
                checkProcessTypeTask).ConfigureAwait(false);

            var transactions = marketDocument.ToTransactions();
            var transactionIdsToBeStored = new List<string>();
            foreach (var transaction in transactions)
            {
                var transactionId = transaction.MarketActivityRecord.Id;
                if (string.IsNullOrEmpty(transactionId))
                {
                    _errors.Add(new EmptyTransactionId());
                    continue;
                }

                if (transactionId.Length != TransactionIdLength)
                {
                    _errors.Add(new InvalidTransactionIdSize(transactionId));
                    continue;
                }

                if (transactionIdsToBeStored.Contains(transactionId))
                {
                    _errors.Add(new DuplicateTransactionIdDetected(transactionId));
                }
                else
                {
                    transactionIdsToBeStored.Add(transaction.MarketActivityRecord.Id);
                }

                if (await TransactionIdIsDuplicatedAsync(messageHeader.SenderId, transactionId, cancellationToken).ConfigureAwait(false))
                {
                    _errors.Add(new DuplicateTransactionIdDetected(transactionId));
                }

                await AddToTransactionQueueAsync(transaction, cancellationToken).ConfigureAwait(false);
            }

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            // We add the transaction id for later comparisons.
            foreach (var transactionId in transactionIdsToBeStored)
            {
                // This should only happen, if the actor has two request running on different threads
                // with non unique transaction id across these threads.
                var success = await TryStoreTransactionIdAsync(
                        messageHeader.SenderId,
                        transactionId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!success)
                {
                    _errors.Add(new DuplicateTransactionIdDetected(transactionId));
                }
            }

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            await TryStoryMessageIdAsync(messageHeader.SenderId, messageHeader.MessageId, cancellationToken).ConfigureAwait(false);

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            await _messageQueueDispatcher.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Succeeded();
        }

        private async Task<bool> TransactionIdIsDuplicatedAsync(string senderId, string transactionId, CancellationToken cancellationToken)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));

            return !await _transactionIds
                .TransactionIdOfSenderIsUniqueAsync(senderId, transactionId, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> TryStoreTransactionIdAsync(string senderId, string transactionId, CancellationToken cancellationToken)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));

            return await _transactionIds
                .TryStoreAsync(senderId, transactionId, cancellationToken).ConfigureAwait(false);
        }

        private Task AddToTransactionQueueAsync(IMarketTransaction transaction, CancellationToken cancellationToken)
        {
            return _messageQueueDispatcher.AddAsync(transaction, cancellationToken);
        }

        private bool MessageIdIsEmpty(string messageId)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (string.IsNullOrEmpty(messageId))
            {
                _errors.Add(new EmptyMessageId());
                return true;
            }

            return false;
        }

        private async Task CheckMessageIdAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (messageId.Length != MessageIdLength)
            {
                _errors.Add(new InvalidMessageIdSize(messageId));
            }
            else if (!await _messageIds.MessageIdIsUniqueForSenderAsync(senderId, messageId, cancellationToken).ConfigureAwait(false))
            {
                _errors.Add(new DuplicateMessageIdDetected(messageId));
            }
        }

        private async Task TryStoryMessageIdAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));
            if (!await _messageIds.TryStoreAsync(senderId, messageId, cancellationToken).ConfigureAwait(false))
            {
                _errors.Add(new DuplicateMessageIdDetected(messageId));
            }
        }

        private async Task CheckMessageTypeAsync(string messageType, CancellationToken cancellationToken)
        {
            var result = await _messageTypeValidator.ValidateAsync(messageType, cancellationToken).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task CheckProcessTypeAsync(string processType, CancellationToken cancellationToken)
        {
            var result = await _processTypeValidator.ValidateAsync(processType, cancellationToken).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task AuthorizeSenderAsync(MessageHeader messageHeader)
        {
            var result = await _senderAuthorizer.AuthorizeAsync(messageHeader.SenderId, messageHeader.SenderRole).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task VerifyReceiverAsync(MessageHeader messageHeader)
        {
            var receiverVerification = await _receiverValidator.VerifyAsync(messageHeader.ReceiverId, messageHeader.ReceiverRole).ConfigureAwait(false);
            _errors.AddRange(receiverVerification.Errors);
        }
    }
}
