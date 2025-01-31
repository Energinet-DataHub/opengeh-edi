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
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;

public class ReceiveIncomingMarketMessage
{
    private readonly IDictionary<(IncomingDocumentType, DocumentFormat), IMessageParser> _messageParsers;
    private readonly ValidateIncomingMessage _validateIncomingMessage;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ILogger<ReceiveIncomingMarketMessage> _logger;
    private readonly IIncomingMessageReceiver _incomingMessageReceiver;
    private readonly DelegateIncomingMessage _delegateIncomingMessage;
    private readonly IClock _clock;
    private readonly AuthenticatedActor _actorAuthenticator;

    public ReceiveIncomingMarketMessage(
        IEnumerable<IMessageParser> messageParsers,
        ValidateIncomingMessage validateIncomingMessage,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient,
        ILogger<ReceiveIncomingMarketMessage> logger,
        IIncomingMessageReceiver incomingMessageReceiver,
        DelegateIncomingMessage delegateIncomingMessage,
        IClock clock,
        AuthenticatedActor actorAuthenticator)
    {
        _messageParsers = messageParsers
            .ToDictionary(
                parser => (parser.DocumentType, parser.DocumentFormat),
                parser => parser);
        _validateIncomingMessage = validateIncomingMessage;
        _responseFactory = responseFactory;
        _archivedMessagesClient = archivedMessagesClient;
        _logger = logger;
        _incomingMessageReceiver = incomingMessageReceiver;
        _delegateIncomingMessage = delegateIncomingMessage;
        _clock = clock;
        _actorAuthenticator = actorAuthenticator;
    }

    public async Task<ResponseMessage> ReceiveIncomingMarketMessageAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        DocumentFormat incomingDocumentFormat,
        IncomingDocumentType documentType,
        DocumentFormat responseDocumentFormat,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        ArgumentNullException.ThrowIfNull(incomingMarketMessageStream);
        var stopwatch = Stopwatch.StartNew();

        var incomingMarketMessageParserResult = await ParseIncomingMessageAsync(
                incomingMarketMessageStream,
                incomingDocumentFormat,
                documentType,
                cancellationToken)
            .ConfigureAwait(false);

        stopwatch.Stop();
        _logger.LogInformation($"IncomingMessage Parsing execution time: {stopwatch.ElapsedMilliseconds} ms");

        if (incomingMarketMessageParserResult.Errors.Count != 0
            || incomingMarketMessageParserResult.IncomingMessage == null)
        {
            var res = Result.Failure([.. incomingMarketMessageParserResult.Errors]);

            _logger.LogInformation(
                "Failed to parse incoming message {DocumentType}. Errors: {Errors}",
                documentType,
                string.Join(',', res.Errors.Select(e => e.ToString())));

            return _responseFactory.From(res, responseDocumentFormat);
        }

        if (ShouldArchive(documentType))
        {
            stopwatch.Restart();
            await ArchiveIncomingMessageAsync(
                    incomingMarketMessageStream,
                    incomingMarketMessageParserResult.IncomingMessage,
                    documentType,
                    cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation($"IncomingMessage Archiving execution time: {stopwatch.ElapsedMilliseconds} ms");
        }

        stopwatch.Restart();
        await _delegateIncomingMessage
            .DelegateAsync(incomingMarketMessageParserResult.IncomingMessage, documentType, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        _logger.LogInformation($"IncomingMessage Delegation execution time: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        var validationResult = await _validateIncomingMessage
            .ValidateAsync(incomingMarketMessageParserResult.IncomingMessage, incomingDocumentFormat, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        _logger.LogInformation($"IncomingMessage Validation execution time: {stopwatch.ElapsedMilliseconds} ms");

        if (!validationResult.Success)
        {
            _logger.LogInformation(
                "Failed to validate incoming message: {MessageId}. Errors: {Errors}",
                incomingMarketMessageParserResult.IncomingMessage?.MessageId,
                string.Join(',', validationResult.Errors.Select(e => e.ToString())));
            return _responseFactory.From(validationResult, responseDocumentFormat);
        }

        stopwatch.Restart();
        var result = await _incomingMessageReceiver
            .ReceiveAsync(
                incomingMarketMessageParserResult.IncomingMessage,
                cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();
        _logger.LogInformation($"IncomingMessage Receiving execution time: {stopwatch.ElapsedMilliseconds} ms");

        if (result.Success)
        {
            return _responseFactory.From(result, responseDocumentFormat);
        }

        _logger.LogInformation(
            "Failed to save incoming message: {MessageId}. Errors: {Errors}",
            incomingMarketMessageParserResult.IncomingMessage!.MessageId,
            string.Join(',', incomingMarketMessageParserResult.Errors.Select(e => e.ToString())));
        return _responseFactory.From(result, responseDocumentFormat);
    }

    private static bool ShouldArchive(IncomingDocumentType documentType)
    {
        return documentType != IncomingDocumentType.NotifyValidatedMeasureData;
    }

    private async Task<IncomingMarketMessageParserResult> ParseIncomingMessageAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        DocumentFormat documentFormat,
        IncomingDocumentType documentType,
        CancellationToken cancellationToken)
    {
        if (_messageParsers.TryGetValue((documentType, documentFormat), out var messageParser))
        {
            return await messageParser.ParseAsync(incomingMarketMessageStream, cancellationToken).ConfigureAwait(false);
        }

        throw new NotSupportedException($"No message parser found for message format '{documentFormat}' and document type '{documentType}'");
    }

    private async Task ArchiveIncomingMessageAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        IIncomingMessage incomingMessage,
        IncomingDocumentType incomingDocumentType,
        CancellationToken cancellationToken)
    {
        var authenticatedActor = _actorAuthenticator.CurrentActorIdentity;
        await _archivedMessagesClient.CreateAsync(
                new ArchivedMessageDto(
                    incomingMessage.MessageId,
                    incomingDocumentType.Name,
                    authenticatedActor.ActorNumber,
                    authenticatedActor.ActorRole,
                    // For RequestAggregatedMeteringData and RequestWholesaleServices,
                    // the receiver is Metered Data Administrator
                    DataHubDetails.DataHubActorNumber,
                    ActorRole.MeteredDataAdministrator,
                    _clock.GetCurrentInstant(),
                    incomingMessage.BusinessReason,
                    ArchivedMessageTypeDto.IncomingMessage,
                    incomingMarketMessageStream),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
