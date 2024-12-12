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

using System.Diagnostics;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.MessageId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.TransactionId;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;

public class ValidateIncomingMessage(
    ISenderAuthorizer senderAuthorizer,
    IReceiverValidator receiverValidator,
    IMessageIdRepository messageIdRepository,
    IMessageTypeValidator messageTypeValidator,
    IProcessTypeValidator processTypeValidator,
    IBusinessTypeValidator businessTypeValidator,
    ITransactionIdRepository transactionIdRepository,
    ILogger<ValidateIncomingMessage> logger)
{
    private const int MaxMessageIdLength = 36;
    private const int MaxTransactionIdLength = 36;

    public async Task<Result> ValidateAsync(
        IIncomingMessage incomingMessage,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessage);

        var errors = (await Task.WhenAll(
                    AuthorizeSenderAsync(incomingMessage),
                    VerifyReceiverAsync(incomingMessage),
                    CheckMessageIdAsync(incomingMessage, cancellationToken),
                    CheckMessageTypeAsync(incomingMessage, cancellationToken),
                    CheckBusinessReasonAsync(incomingMessage, documentFormat, cancellationToken),
                    CheckBusinessTypeAsync(incomingMessage, cancellationToken))
                .ConfigureAwait(false))
            .SelectMany(errs => errs);

        // EF breaks if this is part of the WhenAll above. DbContext detects concurrent access and throws up.
        // It is slightly annoying since all we do is FirstOrDefaultAsync which we await.
        // And read access shouldn't be that much of a problem wrt concurrency.
        // It is also a bit silly to have this as a separate call, but it works.
        var stopwatch = Stopwatch.StartNew();
        var transactionIdErrors =
            await CheckTransactionIdsAsync(incomingMessage, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage CheckTransactionIdsAsync execution time: {stopwatch.ElapsedMilliseconds} ms");

        var allErrors = errors.Concat(transactionIdErrors).ToArray();

        return allErrors.Length > 0 ? Result.Failure(allErrors) : Result.Succeeded();
    }

    private async Task<IReadOnlyCollection<ValidationError>> AuthorizeSenderAsync(IIncomingMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        var allSeriesAreDelegated = message.Series.Count > 0 && message.Series.All(s => s.IsDelegated);

        var result = await senderAuthorizer
            .AuthorizeAsync(message, allSeriesAreDelegated)
            .ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage AuthorizeSenderAsync execution time: {stopwatch.ElapsedMilliseconds} ms");

        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> VerifyReceiverAsync(IIncomingMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        var receiverVerification = await receiverValidator
            .VerifyAsync(message.ReceiverNumber, message.ReceiverRoleCode)
            .ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage VerifyReceiverAsync execution time: {stopwatch.ElapsedMilliseconds} ms");

        return receiverVerification.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckMessageIdAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<ValidationError>();

        if (string.IsNullOrEmpty(message.MessageId))
        {
            errors.Add(new EmptyMessageId());
        }

        if (message.MessageId.Length > MaxMessageIdLength)
        {
            errors.Add(new InvalidMessageIdSize(message.MessageId));
        }
        else if (await messageIdRepository
                     .MessageIdExistsAsync(message.SenderNumber, message.MessageId, cancellationToken)
                     .ConfigureAwait(false))
        {
            errors.Add(new DuplicateMessageIdDetected(message.MessageId));
        }

        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage CheckMessageIdAsync execution time: {stopwatch.ElapsedMilliseconds} ms");

        return errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckMessageTypeAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await messageTypeValidator.ValidateAsync(message, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage CheckMessageTypeAsync execution time: {stopwatch.ElapsedMilliseconds} ms");
        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckBusinessReasonAsync(
        IIncomingMessage message,
        DocumentFormat documentFormat,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await processTypeValidator.ValidateAsync(message, documentFormat, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage CheckBusinessReasonAsync execution time: {stopwatch.ElapsedMilliseconds} ms");
        return result.Errors;
    }

    private async Task<IReadOnlyCollection<ValidationError>> CheckBusinessTypeAsync(
        IIncomingMessage message,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await businessTypeValidator.ValidateAsync(message.BusinessType, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        logger.LogInformation($"ValidateIncomingMessage CheckBusinessTypeAsync execution time: {stopwatch.ElapsedMilliseconds} ms");
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

            var errorsForSeries = CheckTransactionId(
                    transactionId,
                    transactionIdsToBeStored);

            if (errorsForSeries is null)
            {
                transactionIdsToBeStored.Add(transactionId);
            }
            else
            {
                errors.Add(errorsForSeries);
            }
        }

        var duplicatedTransactionIds = await TransactionIdIsDuplicatedAsync(
                message.SenderNumber,
                transactionIdsToBeStored,
                cancellationToken)
            .ConfigureAwait(false);
        foreach (var duplicatedTransactionId in duplicatedTransactionIds)
        {
            errors.Add(new DuplicateTransactionIdDetected(duplicatedTransactionId));
        }

        return errors;
    }

    private ValidationError? CheckTransactionId(
        string transactionId,
        IReadOnlyCollection<string> transactionIdsToBeStored)
    {
        return transactionId switch
        {
            _ when string.IsNullOrEmpty(transactionId) => new EmptyTransactionId(),
            _ when transactionId.Length > MaxTransactionIdLength => new InvalidTransactionIdSize(transactionId),
            _ when transactionIdsToBeStored.Contains(transactionId) =>
                new DuplicateTransactionIdDetected(transactionId),
            _ => null,
        };
    }

    private async Task<IReadOnlyList<string>> TransactionIdIsDuplicatedAsync(
        string senderNumber,
        IReadOnlyList<string> transactionIds,
        CancellationToken cancellationToken)
    {
        return await transactionIdRepository
            .TransactionIdExistsAsync(senderNumber, transactionIds, cancellationToken)
            .ConfigureAwait(false);
    }
}
