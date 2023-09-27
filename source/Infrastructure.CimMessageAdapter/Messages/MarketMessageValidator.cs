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
using Energinet.DataHub.EDI.Application.IncomingMessages;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.Exceptions;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using MessageHeader = Energinet.DataHub.EDI.Application.IncomingMessages.MessageHeader;

namespace Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages
{
    public abstract class MarketMessageValidator
    {
        private const int MessageIdLength = 36;
        private const int TransactionIdLength = 36;
        private readonly List<ValidationError> _errors = new();
        private readonly IMessageIdRepository _messageIdRepository;
        private readonly ITransactionIdRepository _transactionIdRepository;
        private readonly ISenderAuthorizer _senderAuthorizer;
        private readonly IProcessTypeValidator _processTypeValidator;
        private readonly IMessageTypeValidator _messageTypeValidator;
        private readonly IReceiverValidator _receiverValidator;

        protected MarketMessageValidator(
            IMessageIdRepository messageIdRepository,
            ITransactionIdRepository transactionIdRepository,
            ISenderAuthorizer senderAuthorizer,
            IProcessTypeValidator processTypeValidator,
            IMessageTypeValidator messageTypeValidator,
            IReceiverValidator receiverValidator)
        {
            _messageIdRepository = messageIdRepository ?? throw new ArgumentNullException(nameof(messageIdRepository));
            _transactionIdRepository = transactionIdRepository;
            _senderAuthorizer = senderAuthorizer;
            _processTypeValidator = processTypeValidator;
            _messageTypeValidator = messageTypeValidator;
            _receiverValidator = receiverValidator;
        }

        public async Task<Result> ValidateAsync(
            MarketMessage marketMessage,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(marketMessage);

            var authorizeSenderTask = AuthorizeSenderAsync(marketMessage);
            var verifyReceiverTask = VerifyReceiverAsync(marketMessage);
            var checkMessageIdTask = CheckMessageIdAsync(marketMessage.SenderNumber, marketMessage.MessageId, cancellationToken);
            var checkMessageTypeTask = CheckMessageTypeAsync(marketMessage.MessageType, cancellationToken);
            var checkProcessTypeTask = CheckBusinessReasonAsync(marketMessage.BusinessReason, cancellationToken);

            await Task.WhenAll(
                authorizeSenderTask,
                verifyReceiverTask,
                checkMessageIdTask,
                checkMessageTypeTask,
                checkProcessTypeTask).ConfigureAwait(false);

            var transactionIdsToBeStored = new List<string>();
            foreach (var transaction in marketMessage.MarketTransactions)
            {
                var transactionId = transaction.Id;

                if (await CheckTransactionIdAsync(
                        transactionId,
                        marketMessage.SenderNumber,
                        transactionIdsToBeStored,
                        cancellationToken).ConfigureAwait(false))
                {
                    transactionIdsToBeStored.Add(transactionId);
                }
            }

            if (_errors.Count > 0)
            {
                return Result.Failure(_errors.ToArray());
            }

            try
            {
                using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                await _transactionIdRepository.StoreAsync(
                    marketMessage.SenderNumber.Value,
                    transactionIdsToBeStored,
                    cancellationToken).ConfigureAwait(false);
                await _messageIdRepository.StoreAsync(marketMessage.SenderNumber, marketMessage.MessageId, cancellationToken)
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

            return Result.Succeeded();
        }

        private async Task<bool> CheckTransactionIdAsync(string transactionId, ActorNumber senderNumber, List<string> transactionIdsToBeStored, CancellationToken cancellationToken)
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

            if (await TransactionIdIsDuplicatedAsync(senderNumber, transactionId, cancellationToken).ConfigureAwait(false))
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

        private async Task<bool> TransactionIdIsDuplicatedAsync(ActorNumber senderNumber, string transactionId, CancellationToken cancellationToken)
        {
            if (transactionId == null) throw new ArgumentNullException(nameof(transactionId));

            return await _transactionIdRepository
                .TransactionIdExistsAsync(senderNumber.Value, transactionId, cancellationToken).ConfigureAwait(false);
        }

        private async Task CheckMessageIdAsync(ActorNumber senderNumber, string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                _errors.Add(new EmptyMessageId());
            }

            if (messageId.Length != MessageIdLength)
            {
                _errors.Add(new InvalidMessageIdSize(messageId));
            }
            else if (await _messageIdRepository.MessageIdExistsAsync(senderNumber, messageId, cancellationToken).ConfigureAwait(false))
            {
                _errors.Add(new DuplicateMessageIdDetected(messageId));
            }
        }

        private async Task CheckMessageTypeAsync(string messageType, CancellationToken cancellationToken)
        {
            var result = await _messageTypeValidator.ValidateAsync(messageType, cancellationToken).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task CheckBusinessReasonAsync(string businessReason, CancellationToken cancellationToken)
        {
            var result = await _processTypeValidator.ValidateAsync(businessReason, cancellationToken).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task AuthorizeSenderAsync(MarketMessage marketMessage)
        {
            var result = await _senderAuthorizer.AuthorizeAsync(marketMessage.SenderNumber, marketMessage.SenderRole, marketMessage.AuthenticatedUser, marketMessage.AuthenticatedUserRole).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task VerifyReceiverAsync(MarketMessage marketMessage)
        {
            var receiverVerification = await _receiverValidator.VerifyAsync(marketMessage.ReceiverNumber, marketMessage.ReceiverRole).ConfigureAwait(false);
            _errors.AddRange(receiverVerification.Errors);
        }
    }
}
