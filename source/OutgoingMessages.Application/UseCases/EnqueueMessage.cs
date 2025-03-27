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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Enqueue is used by EDI to deliver a message to an appropriate actors queue.
/// </summary>
public class EnqueueMessage
{
    private const int MaxBundleSizeForMeasureData = 2000; // TODO: Get from config

    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IBundleRepository _bundleRepository;
    private readonly IClock _clock;
    private readonly ILogger<EnqueueMessage> _logger;
    private readonly DelegateMessage _delegateMessage;

    public EnqueueMessage(
        IOutgoingMessageRepository outgoingMessageRepository,
        IActorMessageQueueRepository actorMessageQueueRepository,
        IBundleRepository bundleRepository,
        IClock clock,
        ILogger<EnqueueMessage> logger,
        DelegateMessage delegateMessage)
    {
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _bundleRepository = bundleRepository;
        _clock = clock;
        _logger = logger;
        _delegateMessage = delegateMessage;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        messageToEnqueue = await _delegateMessage.DelegateAsync(messageToEnqueue, cancellationToken)
                .ConfigureAwait(false);

        var existingMessage = await _outgoingMessageRepository.GetAsync(
                messageToEnqueue.Receiver,
                messageToEnqueue.ExternalId,
                messageToEnqueue.PeriodStartedAt)
            .ConfigureAwait(false);

        if (existingMessage != null) // Message is already enqueued, do nothing (idempotency check)
            return existingMessage.Id;

        var actorMessageQueueId = await GetMessageQueueIdForReceiverAsync(
                messageToEnqueue.GetActorMessageQueueMetadata(),
                cancellationToken)
            .ConfigureAwait(false);

        await BundleMessageAsync(messageToEnqueue, actorMessageQueueId, cancellationToken)
            .ConfigureAwait(false);

        // Add to outgoing message repository (and upload to file storage) after adding actor message queue and bundle,
        // to minimize the cases where a message is uploaded to file storage but adding actor message queue fails
        await _outgoingMessageRepository.AddAsync(messageToEnqueue).ConfigureAwait(false);

        _logger.LogInformation(
            "Enqueued message for OutgoingMessageId: {OutgoingMessageId} for ActorNumber: {ActorNumber} ActorRole: {ActorRole}, for Received Event id: {EventId}",
            messageToEnqueue.Id,
            messageToEnqueue.Receiver.Number.Value,
            messageToEnqueue.Receiver.ActorRole.Code,
            messageToEnqueue.EventId);

        return messageToEnqueue.Id;
    }

    /// <summary>
    /// Bundle message for the receiver. Only NotifyValidatedMeasureData messages has bundles with more than one
    /// message in each, the rest of the messages has a bundle size of 1 (effectively not bundled).
    /// </summary>
    private async Task BundleMessageAsync(OutgoingMessage messageToEnqueue, ActorMessageQueueId actorMessageQueueId, CancellationToken cancellationToken)
    {
        Bundle? bundle = null;
        var maxBundleSize = GetMaxBundleSize(messageToEnqueue.DocumentType);

        // No need to find existing bundle if max bundle size is 1
        if (maxBundleSize > 1)
        {
            // Get existing bundle.
            bundle = await _bundleRepository.GetOpenBundleAsync(
                    messageToEnqueue.DocumentType,
                    BusinessReason.FromName(messageToEnqueue.BusinessReason),
                    actorMessageQueueId,
                    GetRelatedToMessageIdForBundling(messageToEnqueue),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Create bundle if not exists.
        if (bundle == null)
            bundle = CreateBundle(messageToEnqueue, actorMessageQueueId, maxBundleSize);

        // Add message to bundle and increment counter by 1.
        //   - Close bundle if it is full (already handled in the .Add() method).
        //   - This will fail on commit changes (intentionally) because of RowVersion, if the bundle is incremented or
        //     closed concurrently.
        bundle.Add(messageToEnqueue);
    }

    private Bundle CreateBundle(OutgoingMessage messageToEnqueue, ActorMessageQueueId actorMessageQueueId, int maxBundleSize)
    {
        var newBundle = new Bundle(
            actorMessageQueueId: actorMessageQueueId,
            businessReason: BusinessReason.FromName(messageToEnqueue.BusinessReason),
            documentTypeInBundle: messageToEnqueue.DocumentType,
            maxNumberOfMessagesInABundle: maxBundleSize,
            created: _clock.GetCurrentInstant(),
            relatedToMessageId: GetRelatedToMessageIdForBundling(messageToEnqueue));

        _bundleRepository.Add(newBundle);

        return newBundle;
    }

    private async Task<ActorMessageQueueId> GetMessageQueueIdForReceiverAsync(Receiver receiver, CancellationToken cancellationToken)
    {
        var actorMessageQueueId = await _actorMessageQueueRepository.ActorMessageQueueIdForAsync(
            receiver.Number,
            receiver.ActorRole,
            cancellationToken).ConfigureAwait(false);

        if (actorMessageQueueId == null)
        {
            _logger.LogInformation("Creating new message queue for Actor: {ActorNumber}, ActorRole: {ActorRole}", receiver.Number.Value, receiver.ActorRole.Name);
            var actorMessageQueueToCreate = ActorMessageQueue.CreateFor(receiver);
            _actorMessageQueueRepository.Add(actorMessageQueueToCreate);
            actorMessageQueueId = actorMessageQueueToCreate.Id;
        }

        return actorMessageQueueId;
    }

    private int GetMaxBundleSize(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyValidatedMeasureData
            ? MaxBundleSizeForMeasureData
            : 1;
    }

    private MessageId? GetRelatedToMessageIdForBundling(OutgoingMessage messageToEnqueue)
    {
        // RSM-012 NotifyValidatedMeasureData messages can be bundled with different related to message id's,
        // so the bundle should not contain a related to message id for that document type.
        return messageToEnqueue.DocumentType == DocumentType.NotifyValidatedMeasureData
            ? null
            : messageToEnqueue.RelatedToMessageId;
    }
}
