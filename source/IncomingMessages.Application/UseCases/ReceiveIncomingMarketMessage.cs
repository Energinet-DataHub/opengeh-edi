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

using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;

public class ReceiveIncomingMarketMessage
{
    private readonly MarketMessageParser _marketMessageParser;
    private readonly ValidateIncomingMessage _validateIncomingMessage;
    private readonly ResponseFactory _responseFactory;
    private readonly IArchivedMessagesClient _archivedMessagesClient;
    private readonly ILogger<IncomingMessageClient> _logger;
    private readonly IIncomingMessageReceiver _incomingMessageReceiver;
    private readonly DelegateIncomingMessage _delegateIncomingMessage;
    private readonly IClock _clock;
    private readonly AuthenticatedActor _actorAuthenticator;

    public ReceiveIncomingMarketMessage(
        MarketMessageParser marketMessageParser,
        ValidateIncomingMessage validateIncomingMessage,
        ResponseFactory responseFactory,
        IArchivedMessagesClient archivedMessagesClient,
        ILogger<IncomingMessageClient> logger,
        IIncomingMessageReceiver incomingMessageReceiver,
        DelegateIncomingMessage delegateIncomingMessage,
        IClock clock,
        AuthenticatedActor actorAuthenticator)
    {
        _marketMessageParser = marketMessageParser;
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

        var incomingMarketMessageParserResult =
            await _marketMessageParser.ParseAsync(incomingMarketMessageStream, incomingDocumentFormat, documentType, cancellationToken)
                .ConfigureAwait(false);

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
            await ArchiveIncomingMessageAsync(
                    incomingMarketMessageStream,
                    incomingMarketMessageParserResult.IncomingMessage,
                    documentType,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await _delegateIncomingMessage
                .DelegateAsync(incomingMarketMessageParserResult.IncomingMessage, documentType, cancellationToken)
                .ConfigureAwait(false);

        var validationResult = await _validateIncomingMessage
            .ValidateAsync(incomingMarketMessageParserResult.IncomingMessage, cancellationToken)
            .ConfigureAwait(false);

        if (!validationResult.Success)
        {
            _logger.LogInformation(
                "Failed to validate incoming message: {MessageId}. Errors: {Errors}",
                incomingMarketMessageParserResult.IncomingMessage?.MessageId,
                string.Join(',', validationResult.Errors.Select(e => e.ToString())));
            return _responseFactory.From(validationResult, responseDocumentFormat);
        }

        var result = await _incomingMessageReceiver
            .ReceiveAsync(
                    incomingMarketMessageParserResult.IncomingMessage,
                    cancellationToken)
            .ConfigureAwait(false);

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
        return documentType != IncomingDocumentType.MeteredDataForMeasurementPoint;
    }

    private async Task ArchiveIncomingMessageAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        IIncomingMessage incomingMessage,
        IncomingDocumentType incomingDocumentType,
        CancellationToken cancellationToken)
    {
        var authenticatedActor = _actorAuthenticator.CurrentActorIdentity;
        await _archivedMessagesClient.CreateAsync(
            new ArchivedMessage(
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
                ArchivedMessageType.IncomingMessage,
                incomingMarketMessageStream),
            cancellationToken).ConfigureAwait(false);
    }
}
