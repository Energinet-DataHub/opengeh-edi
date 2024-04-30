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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageValidators
{
    public class RequestAggregatedMeasureDataMessageValidator
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
        private readonly IBusinessTypeValidator _businessTypeValidator;

        public RequestAggregatedMeasureDataMessageValidator(
            IMessageIdRepository messageIdRepository,
            ITransactionIdRepository transactionIdRepository,
            ISenderAuthorizer senderAuthorizer,
            IProcessTypeValidator processTypeValidator,
            IMessageTypeValidator messageTypeValidator,
            IReceiverValidator receiverValidator,
            IBusinessTypeValidator businessTypeValidator)
        {
            _messageIdRepository = messageIdRepository;
            _transactionIdRepository = transactionIdRepository;
            _senderAuthorizer = senderAuthorizer;
            _processTypeValidator = processTypeValidator;
            _messageTypeValidator = messageTypeValidator;
            _receiverValidator = receiverValidator;
            _businessTypeValidator = businessTypeValidator;
        }

        public async Task<Result> ValidateAsync(
            RequestAggregatedMeasureDataMessage requestAggregatedMeasureDataMessage,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(requestAggregatedMeasureDataMessage);

            var authorizeSenderTask = AuthorizeSenderAsync(requestAggregatedMeasureDataMessage);
            var verifyReceiverTask = VerifyReceiverAsync(requestAggregatedMeasureDataMessage);
            var checkMessageIdTask = CheckMessageIdAsync(requestAggregatedMeasureDataMessage.SenderNumber, requestAggregatedMeasureDataMessage.MessageId, cancellationToken);
            var checkMessageTypeTask = CheckMessageTypeAsync(requestAggregatedMeasureDataMessage.MessageType, cancellationToken);
            var checkProcessTypeTask = CheckBusinessReasonAsync(requestAggregatedMeasureDataMessage.BusinessReason, cancellationToken);
            var checkBusinessTypeTask = CheckBusinessTypeAsync(requestAggregatedMeasureDataMessage.BusinessType, cancellationToken);

            await Task.WhenAll(
                authorizeSenderTask,
                verifyReceiverTask,
                checkMessageIdTask,
                checkMessageTypeTask,
                checkProcessTypeTask,
                checkBusinessTypeTask).ConfigureAwait(false);

            var transactionIdsToBeStored = new List<string>();
            foreach (var serie in requestAggregatedMeasureDataMessage.Series)
            {
                var transactionId = serie.TransactionId;

                if (await CheckTransactionIdAsync(
                        transactionId,
                        requestAggregatedMeasureDataMessage.SenderNumber,
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

            return Result.Succeeded();
        }

        private async Task<bool> CheckTransactionIdAsync(string transactionId, string senderNumber, List<string> transactionIdsToBeStored, CancellationToken cancellationToken)
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

        private async Task<bool> TransactionIdIsDuplicatedAsync(string senderNumber, string transactionId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(transactionId);

            return await _transactionIdRepository
                .TransactionIdExistsAsync(senderNumber, transactionId, cancellationToken).ConfigureAwait(false);
        }

        private async Task CheckMessageIdAsync(string senderNumber, string messageId, CancellationToken cancellationToken)
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

        private async Task AuthorizeSenderAsync(RequestAggregatedMeasureDataMessage message)
        {
            var allSeriesAreDelegated = message.Series.Count > 0 && message.Series.All(s => s.IsDelegated);

            var result = await _senderAuthorizer.AuthorizeAsync(message.SenderNumber, message.SenderRoleCode, allSeriesAreDelegated).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }

        private async Task VerifyReceiverAsync(RequestAggregatedMeasureDataMessage message)
        {
            var receiverVerification = await _receiverValidator.VerifyAsync(message.ReceiverNumber, message.ReceiverRoleCode).ConfigureAwait(false);
            _errors.AddRange(receiverVerification.Errors);
        }

        private async Task CheckBusinessTypeAsync(string? businessType, CancellationToken cancellationToken)
        {
            var result = await _businessTypeValidator.ValidateAsync(businessType, cancellationToken).ConfigureAwait(false);
            _errors.AddRange(result.Errors);
        }
    }
}
