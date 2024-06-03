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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

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
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly IArchivedMessagesClient _archivedMessageClient;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public PeekMessage(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IMarketDocumentRepository marketDocumentRepository,
        DocumentFactory documentFactory,
        IOutgoingMessageRepository outgoingMessageRepository,
        ActorMessageQueueContext actorMessageQueueContext,
        IArchivedMessagesClient archivedMessageClient,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _marketDocumentRepository = marketDocumentRepository;
        _documentFactory = documentFactory;
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueContext = actorMessageQueueContext;
        _archivedMessageClient = archivedMessageClient;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task<PeekResultDto> PeekAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack)
        {
            request = request with { ActorRole = request.ActorRole.ForActorMessageQueue(), };
        }

        await PeekAndCommitToEnsureBundleIsClosedAsync(request).ConfigureAwait(false);

        var actorMessageQueue = await
            _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);

        if (actorMessageQueue is null)
            return new PeekResultDto(null, null);

        var peekResult = request.DocumentFormat == DocumentFormat.Ebix ? actorMessageQueue.Peek() : actorMessageQueue.Peek(request.MessageCategory);

        if (peekResult.BundleId == null)
            return new PeekResultDto(null, null);

        if (peekResult.MessageId == null)
            return new PeekResultDto(null, null);

        var marketDocument = await _marketDocumentRepository.GetAsync(peekResult.BundleId).ConfigureAwait(false);

        if (marketDocument == null)
        {
            var timestamp = _systemDateTimeProvider.Now();

            var outgoingMessageBundle = await _outgoingMessageRepository.GetAsync(peekResult.BundleId, peekResult.MessageId).ConfigureAwait(false);
            var marketDocumentStream = await _documentFactory.CreateFromAsync(outgoingMessageBundle, request.DocumentFormat, timestamp).ConfigureAwait(false);

            var archivedMessageToCreate = new ArchivedMessage(
                peekResult.MessageId.Value.Value,
                outgoingMessageBundle.OutgoingMessages.Select(om => om.EventId).ToArray(),
                outgoingMessageBundle.DocumentType.ToString(),
                outgoingMessageBundle.SenderId.Value,
                outgoingMessageBundle.Receiver.Number.Value,
                timestamp,
                outgoingMessageBundle.BusinessReason,
                ArchivedMessageType.OutgoingMessage,
                marketDocumentStream,
                outgoingMessageBundle.RelatedToMessageId);

            var archivedFile = await _archivedMessageClient.CreateAsync(archivedMessageToCreate, cancellationToken).ConfigureAwait(false);

            marketDocument = new MarketDocument(peekResult.BundleId, archivedFile);
            _marketDocumentRepository.Add(marketDocument);
        }

        return new PeekResultDto(marketDocument.GetMarketDocumentStream().Stream, peekResult.MessageId);
    }

    private async Task PeekAndCommitToEnsureBundleIsClosedAsync(PeekRequestDto request)
    {
        // Right after we call Peek(), we close the bundle. This is to ensure that the bundle wont be added more messages, after we have peeked.
        // And before we are able to update the bundle to closed in the database.
        var actorMessageQueue = await
            _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);
        var peekResult = actorMessageQueue?.Peek(request.MessageCategory);
        if (peekResult != null)
            await _actorMessageQueueContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
