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

using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageValidators;

public class IncomingMessageValidator
{
    private const int MaxMessageIdLength = 36;
    private const int MaxTransactionIdLength = 36;

    private readonly ISenderAuthorizer _senderAuthorizer;
    private readonly IReceiverValidator _receiverValidator;
    private readonly IMessageIdRepository _messageIdRepository;
    private readonly IMessageTypeValidator _messageTypeValidator;
    private readonly IProcessTypeValidator _processTypeValidator;
    private readonly IBusinessTypeValidator _businessTypeValidator;
    private readonly ITransactionIdRepository _transactionIdRepository;

    public IncomingMessageValidator(
        ISenderAuthorizer senderAuthorizer,
        IReceiverValidator receiverValidator,
        IMessageIdRepository messageIdRepository,
        IMessageTypeValidator messageTypeValidator,
        IProcessTypeValidator processTypeValidator,
        IBusinessTypeValidator businessTypeValidator,
        ITransactionIdRepository transactionIdRepository)
    {
        _senderAuthorizer = senderAuthorizer;
        _receiverValidator = receiverValidator;
        _messageIdRepository = messageIdRepository;
        _messageTypeValidator = messageTypeValidator;
        _processTypeValidator = processTypeValidator;
        _businessTypeValidator = businessTypeValidator;
        _transactionIdRepository = transactionIdRepository;
    }

    public async Task<Result> ValidateAsync(
        IIncomingMessage incomingMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessage);

        var errors = (await Task.WhenAll(
                    AuthorizeSenderAsync(incomingMessage),
                    VerifyReceiverAsync(incomingMessage),
                    CheckMessageIdAsync(incomingMessage, cancellationToken),
                    CheckMessageTypeAsync(incomingMessage, cancellationToken),
                    CheckBusinessReasonAsync(incomingMessage, cancellationToken),
                    CheckBusinessTypeAsync(incomingMessage, cancellationToken))
                .ConfigureAwait(false))
            .SelectMany(errs => errs);

        // EF breaks if this is part of the WhenAll above. DbContext detects concurrent access and throws up.
        // It is slightly annoying since all we do is FirstOrDefaultAsync which we await.
        // And read access shouldn't be that much of a problem wrt concurrency.
        // It is also a bit silly to have this as a separate call, but it works.
        var transactionIdErrors =
            await CheckTransactionIdsAsync(incomingMessage, cancellationToken).ConfigureAwait(false);

        var allErrors = errors.Concat(transactionIdErrors).ToArray();

        return allErrors.Length > 0 ? Result.Failure(allErrors) : Result.Succeeded();
    }

    private async Task<IReadOnlyCollection<ValidationError>> AuthorizeSenderAsync(IIncomingMessage message)
    {
        var allSeriesAreDelegated = message.Series.Count > 0 && message.Series.All(s => s.IsDelegated);

        var result = await _senderAuthorizer
            .AuthorizeAsync(message, allSeriesAreDelegated)
            .ConfigureAwait(false);

        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> VerifyReceiverAsync(IIncomingMessage message)
    {
        var receiverVerification = await _receiverValidator
            .VerifyAsync(message.ReceiverNumber, message.ReceiverRoleCode)
            .ConfigureAwait(false);

        return receiverVerification.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckMessageIdAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrEmpty(message.MessageId))
        {
            errors.Add(new EmptyMessageId());
        }

        if (message.MessageId.Length > MaxMessageIdLength)
        {
            errors.Add(new InvalidMessageIdSize(message.MessageId));
        }
        else if (await _messageIdRepository
                     .MessageIdExistsAsync(message.SenderNumber, message.MessageId, cancellationToken)
                     .ConfigureAwait(false))
        {
            errors.Add(new DuplicateMessageIdDetected(message.MessageId));
        }

        return errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckMessageTypeAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var result = await _messageTypeValidator.ValidateAsync(message, cancellationToken)
            .ConfigureAwait(false);
        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckBusinessReasonAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var result = await _processTypeValidator.ValidateAsync(message, cancellationToken)
            .ConfigureAwait(false);
        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckBusinessTypeAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var result = await _businessTypeValidator.ValidateAsync(message.BusinessType, cancellationToken)
            .ConfigureAwait(false);
        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckTransactionIdsAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var errors = new List<ValidationError>();
        var transactionIdsToBeStored = new List<string>();

        foreach (var series in message.Series)
        {
            var transactionId = series.TransactionId;

            var errorsForSeries = await CheckTransactionIdAsync(
                    transactionId,
                    message.SenderNumber,
                    transactionIdsToBeStored,
                    cancellationToken)
                .ConfigureAwait(false);

            if (errorsForSeries is null)
            {
                transactionIdsToBeStored.Add(transactionId);
            }
            else
            {
                errors.Add(errorsForSeries);
            }
        }

        return errors;
    }

    private async Task<ValidationError?> CheckTransactionIdAsync(
        string transactionId,
        string senderNumber,
        IReadOnlyCollection<string> transactionIdsToBeStored,
        CancellationToken cancellationToken)
    {
        return transactionId switch
        {
            _ when string.IsNullOrEmpty(transactionId) => new EmptyTransactionId(),
            _ when transactionId.Length > MaxTransactionIdLength => new InvalidTransactionIdSize(transactionId),
            _ when await TransactionIdIsDuplicatedAsync(senderNumber, transactionId, cancellationToken)
                .ConfigureAwait(false) => new DuplicateTransactionIdDetected(transactionId),
            _ when transactionIdsToBeStored.Contains(transactionId) =>
                new DuplicateTransactionIdDetected(transactionId),
            _ => null,
        };
    }

    private async Task<bool> TransactionIdIsDuplicatedAsync(
        string senderNumber,
        string transactionId,
        CancellationToken cancellationToken)
    {
        return await _transactionIdRepository
            .TransactionIdExistsAsync(senderNumber, transactionId, cancellationToken)
            .ConfigureAwait(false);
    }
}
