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
using System.Transactions;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Exceptions;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Queues;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.MarketTransactions;

namespace Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages
{
    public abstract class MessageReceiver<TQueue>
        where TQueue : Queue
    {
        private const int MessageIdLength = 36;
        private const int TransactionIdLength = 36;
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIds _messageIds;
        private readonly IMessageQueueDispatcher<TQueue> _messageQueueDispatcher;
        private readonly ITransactionIdRepository _transactionIdRepository;
        private readonly ISenderAuthorizer _senderAuthorizer;
        private readonly IProcessTypeValidator _processTypeValidator;
        private readonly IMessageTypeValidator _messageTypeValidator;
        private readonly IReceiverValidator _receiverValidator;

        protected MessageReceiver(
            IMessageIds messageIds,
            IMessageQueueDispatcher<TQueue> messageQueueDispatcher,
            ITransactionIdRepository transactionIdRepository,
            ISenderAuthorizer senderAuthorizer,
            IProcessTypeValidator processTypeValidator,
            IMessageTypeValidator messageTypeValidator,
            IReceiverValidator receiverValidator)
        {
            _messageIds = messageIds ?? throw new ArgumentNullException(nameof(messageIds));
            _messageQueueDispatcher = messageQueueDispatcher ??
                                             throw new ArgumentNullException(nameof(messageQueueDispatcher));
            _transactionIdRepository = transactionIdRepository;
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

                if (await CheckTransactionIdAsync(
                        transactionId,
                        messageHeader.SenderId,
                        transactionIdsToBeStored,
                        cancellationToken).ConfigureAwait(false))
                {
                    transactionIdsToBeStored.Add(transactionId);
                    await AddToTransactionQueueAsync(transaction, cancellationToken).ConfigureAwait(false);
                }
            }

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                var senderId = messageHeader.SenderId;
                await _transactionIdRepository.StoreAsync(
                    senderId,
                    transactionIdsToBeStored,
                    cancellationToken).ConfigureAwait(false);
                await _messageIds.StoreAsync(senderId, messageHeader.MessageId, cancellationToken)
                    .ConfigureAwait(false);

                scope.Complete();
            }
            catch (NotSuccessfulTransactionIdsStorageException)
            {
                _errors.Add(new DuplicateTransactionIdDetected());
            }
            catch (NotSuccessfulMessageIdStorageException e)
            {
                _errors.Add(new DuplicateMessageIdDetected(e.MessageId));
            }

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            await _messageQueueDispatcher.CommitAsync(cancellationToken).ConfigureAwait(false);
            return Result.Succeeded();
        }

        private async Task<bool> CheckTransactionIdAsync(string transactionId, string senderId, List<string> transactionIdsToBeStored, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                _errors.Add(new EmptyTransactionId());
                return false;
            }

            if (transactionId.Length != TransactionIdLength)
            {
                _errors.Add(new InvalidTransactionIdSize(transactionId));
                return false;
            }

            if (await TransactionIdIsDuplicatedAsync(senderId, transactionId, cancellationToken).ConfigureAwait(false))
            {
                _errors.Add(new DuplicateTransactionIdDetected(transactionId));
                return false;
            }

            if (transactionIdsToBeStored.Contains(transactionId))
            {
                _errors.Add(new DuplicateTransactionIdDetected(transactionId));
                return false;
            }

            return true;
        }

        private async Task<bool> TransactionIdIsDuplicatedAsync(string senderId, string transactionId, CancellationToken cancellationToken)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));

            return await _transactionIdRepository
                .TransactionIdExistsAsync(senderId, transactionId, cancellationToken).ConfigureAwait(false);
        }

        private Task AddToTransactionQueueAsync(IMarketTransaction transaction, CancellationToken cancellationToken)
        {
            return _messageQueueDispatcher.AddAsync(transaction, cancellationToken);
        }

        private async Task CheckMessageIdAsync(string senderId, string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                _errors.Add(new EmptyMessageId());
            }

            if (messageId.Length != MessageIdLength)
            {
                _errors.Add(new InvalidMessageIdSize(messageId));
            }
            else if (await _messageIds.MessageIdExistsAsync(senderId, messageId, cancellationToken).ConfigureAwait(false))
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
