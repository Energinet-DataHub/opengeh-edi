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

using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using MediatR;
using NodaTime;
using PeekResult = Energinet.DataHub.EDI.ActorMessageQueue.Contracts.PeekResult;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;

public class PeekHandler : IRequestHandler<PeekCommand, PeekResult>
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IMarketDocumentRepository _marketDocumentRepository;
    private readonly DocumentFactory _documentFactory;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly IArchivedMessageRepository _archivedMessageRepository;

    public PeekHandler(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IMarketDocumentRepository marketDocumentRepository,
        DocumentFactory documentFactory,
        IOutgoingMessageRepository outgoingMessageRepository,
        ActorMessageQueueContext actorMessageQueueContext,
        IArchivedMessageRepository archivedMessageRepository)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _marketDocumentRepository = marketDocumentRepository;
        _documentFactory = documentFactory;
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueContext = actorMessageQueueContext;
        _archivedMessageRepository = archivedMessageRepository;
    }

    public async Task<PeekResult> Handle(PeekCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await EnsureBundleIsClosedBeforePeekingAsync(request).ConfigureAwait(false);

        var actorMessageQueue = await
            _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);
        if (actorMessageQueue is null)
            return new PeekResult(null);

        var peekResult = request.DocumentFormat == DocumentFormat.Ebix ? actorMessageQueue.Peek() : actorMessageQueue.Peek(request.MessageCategory);

        if (peekResult.BundleId == null)
            return new PeekResult(null);

        var document = await _marketDocumentRepository.GetAsync(peekResult.BundleId).ConfigureAwait(false);

        if (document == null)
        {
            var timestamp = SystemClock.Instance.GetCurrentInstant();

            var outgoingMessageBundle = await _outgoingMessageRepository.GetAsync(peekResult.BundleId).ConfigureAwait(false);
            var result = await _documentFactory.CreateFromAsync(outgoingMessageBundle, request.DocumentFormat, timestamp).ConfigureAwait(false);

            document = new MarketDocument(result, peekResult.BundleId);
            await _marketDocumentRepository.AddAsync(document).ConfigureAwait(false);

            _archivedMessageRepository.Add(new ArchivedMessage(
                peekResult.BundleId.Id.ToString(),
                peekResult.BundleId.Id.ToString(),
                outgoingMessageBundle.DocumentType.ToString(),
                outgoingMessageBundle.SenderId.Value,
                outgoingMessageBundle.Receiver.Number.Value,
                timestamp,
                outgoingMessageBundle.BusinessReason,
                result));
        }

        return new PeekResult(document.Payload, document.BundleId.Id);
    }

    private async Task EnsureBundleIsClosedBeforePeekingAsync(PeekCommand request)
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
