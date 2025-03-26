﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Application.Mapping;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Microsoft.ApplicationInsights;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Peek is used by the actor to peek at the next message in their queue.
/// </summary>
public class PeekMessage
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IMarketDocumentRepository _marketDocumentRepository;
    private readonly DocumentFactory _documentFactory;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IActorMessageQueueContext _actorMessageQueueContext;
    private readonly IArchivedMessagesClient _archivedMessageClient;
    private readonly IClock _clock;
    private readonly IBundleRepository _bundleRepository;
    private readonly AuthenticatedActor _actorAuthenticator;
    private readonly TelemetryClient _telemetryClient;

    public PeekMessage(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IMarketDocumentRepository marketDocumentRepository,
        DocumentFactory documentFactory,
        IOutgoingMessageRepository outgoingMessageRepository,
        IActorMessageQueueContext actorMessageQueueContext,
        IArchivedMessagesClient archivedMessageClient,
        IClock clock,
        IBundleRepository bundleRepository,
        AuthenticatedActor actorAuthenticator,
        TelemetryClient telemetryClient)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _marketDocumentRepository = marketDocumentRepository;
        _documentFactory = documentFactory;
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueContext = actorMessageQueueContext;
        _archivedMessageClient = archivedMessageClient;
        _clock = clock;
        _bundleRepository = bundleRepository;
        _actorAuthenticator = actorAuthenticator;
        _telemetryClient = telemetryClient;
    }

    public async Task<PeekResultDto?> PeekAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack)
        {
            request = request with { ActorRole = request.ActorRole.ForActorMessageQueue(), };
        }

        var actorMessageQueue = await
            _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole, cancellationToken).ConfigureAwait(false);

        if (actorMessageQueue is null)
        {
            return null;
        }

        var bundle = await CloseBundleAndCommitAsync(request, actorMessageQueue.Id, cancellationToken).ConfigureAwait(false);

        if (bundle is null)
        {
            return null;
        }

        bundle.Peek();

        var peekResult = new PeekResult(bundle.Id, bundle.MessageId);

        var marketDocument = await _marketDocumentRepository.GetAsync(peekResult.BundleId, cancellationToken).ConfigureAwait(false);

        marketDocument ??= await GenerateMarketDocumentAsync(request, cancellationToken, peekResult).ConfigureAwait(false);

        return new PeekResultDto(marketDocument.GetMarketDocumentStream().Stream, peekResult.MessageId);
    }

    private async Task<MarketDocument> GenerateMarketDocumentAsync(
        PeekRequestDto request,
        CancellationToken cancellationToken,
        PeekResult peekResult)
    {
        MarketDocument marketDocument;
        var timestamp = _clock.GetCurrentInstant();

        var outgoingMessageBundle = await _outgoingMessageRepository.GetAsync(peekResult, cancellationToken).ConfigureAwait(false);
        var marketDocumentStream = await _documentFactory.CreateFromAsync(outgoingMessageBundle, request.DocumentFormat, timestamp, cancellationToken).ConfigureAwait(false);

        var authenticatedActor = _actorAuthenticator.CurrentActorIdentity;
        var archivedMessageToCreate = new ArchivedMessageDto(
            messageId: outgoingMessageBundle.MessageId.Value,
            eventIds: outgoingMessageBundle.OutgoingMessages.Select(om => om.EventId).ToArray(),
            documentType: outgoingMessageBundle.DocumentType.ToString(),
            senderNumber: outgoingMessageBundle.SenderId,
            senderRole: outgoingMessageBundle.SenderRole,
            // The receiver is always the authenticated actor
            receiverNumber: authenticatedActor.ActorNumber,
            receiverRole: authenticatedActor.ActorRole,
            createdAt: timestamp,
            businessReason: BusinessReason.FromName(outgoingMessageBundle.BusinessReason).Code,
            archivedMessageType: ArchivedMessageTypeDto.OutgoingMessage,
            marketDocumentStream: marketDocumentStream,
            relatedToMessageId: outgoingMessageBundle.RelatedToMessageId);

        var archivedFile = await _archivedMessageClient.CreateAsync(archivedMessageToCreate, cancellationToken).ConfigureAwait(false);

        marketDocument = new MarketDocument(peekResult.BundleId, archivedFile);
        _marketDocumentRepository.Add(marketDocument);

        var logName = MetricNameMapper.MessageGenerationMetricName(
            outgoingMessageBundle.DocumentType,
            request.DocumentFormat,
            outgoingMessageBundle.RelatedToMessageId != null);

        _telemetryClient
            .GetMetric(logName)
            .TrackValue(1);

        return marketDocument;
    }

    private async Task<Bundle?> CloseBundleAndCommitAsync(PeekRequestDto request, ActorMessageQueueId actorMessageQueueId, CancellationToken cancellationToken)
    {
        // Right after we call Close(), we close the bundle. This is to ensure that messages won't be added to
        // the bundle after we have peeked and before we are able to update the bundle to closed in the database.
        var bundle = await _bundleRepository
            .GetOldestBundleAsync(actorMessageQueueId, request.MessageCategory, cancellationToken)
            .ConfigureAwait(false);

        return bundle;
    }
}
