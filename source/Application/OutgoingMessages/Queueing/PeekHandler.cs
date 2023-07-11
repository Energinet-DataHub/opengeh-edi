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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.Commands.Commands;
using Application.Configuration.DataAccess;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages.Peek;
using Domain.OutgoingMessages.Queueing;
using MediatR;
using PeekResult = Application.OutgoingMessages.Peek.PeekResult;

namespace Application.OutgoingMessages.Queueing;

public class PeekHandler : IRequestHandler<PeekCommand, PeekResult>
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IMarketDocumentRepository _marketDocumentRepository;
    private readonly DocumentFactory _documentFactory;
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public PeekHandler(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IMarketDocumentRepository marketDocumentRepository,
        DocumentFactory documentFactory,
        IOutgoingMessageStore outgoingMessageStore,
        ISystemDateTimeProvider systemDateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _marketDocumentRepository = marketDocumentRepository;
        _documentFactory = documentFactory;
        _outgoingMessageStore = outgoingMessageStore;
        _systemDateTimeProvider = systemDateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<PeekResult> Handle(PeekCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actorMessageQueue = await
            _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);

        if (actorMessageQueue is null)
            return new PeekResult(null);

        var peekResult = actorMessageQueue.Peek(request.MessageCategory);
        // Right after we Peek, we close the bundle. This is to ensure that the bundle wont be added more messages, after we have peeked.
        // And before we potentially create a document from the bundle and are able to return it.
        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        if (peekResult.BundleId == null)
            return new PeekResult(null);

        var document = await _marketDocumentRepository.GetAsync(peekResult.BundleId).ConfigureAwait(false);

        if (document == null)
        {
            var outgoingMessages = await _outgoingMessageStore.GetByAssignedBundleIdAsync(peekResult.BundleId).ConfigureAwait(false);
            var result = await _documentFactory.CreateFromAsync(outgoingMessages, request.DocumentFormat, _systemDateTimeProvider.Now()).ConfigureAwait(false);
            document = new MarketDocument(result, peekResult.BundleId);
            await _marketDocumentRepository.AddAsync(document).ConfigureAwait(false);
        }

        return new PeekResult(document!.Payload, document.BundleId.Id);
    }
}

public record PeekCommand(ActorNumber ActorNumber, MessageCategory MessageCategory, MarketRole ActorRole, DocumentFormat DocumentFormat) : ICommand<PeekResult>;
