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

using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Enqueue is used by EDI to deliver a message to an appropriate actors queue.
/// </summary>
public class EnqueueMessage
{
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IBundleRepository _bundleRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<EnqueueMessage> _logger;
    private readonly DelegateMessage _delegateMessage;

    public EnqueueMessage(
        IOutgoingMessageRepository outgoingMessageRepository,
        IActorMessageQueueRepository actorMessageQueueRepository,
        IBundleRepository bundleRepository,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<EnqueueMessage> logger,
        DelegateMessage delegateMessage)
    {
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _bundleRepository = bundleRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
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

        var existingMessage = await _outgoingMessageRepository.GetAsync(messageToEnqueue.Receiver, messageToEnqueue.ExternalId).ConfigureAwait(false);
        if (existingMessage != null) // Message is already enqueued, do nothing (idempotency check)
            return existingMessage.Id;

        var actorMessageQueueId = await GetMessageQueueIdForReceiverAsync(messageToEnqueue.GetActorMessageQueueMetadata())
                .ConfigureAwait(false);

        var newBundle = CreateBundle(messageToEnqueue, actorMessageQueueId);
        newBundle.Add(messageToEnqueue);

        // Add to outgoing message repository (and upload to file storage) after adding actor message queue and bundle,
        // to minimize the cases where a message is uploaded to file storage but adding actor message queue fails
        await _outgoingMessageRepository.AddAsync(messageToEnqueue).ConfigureAwait(false);

        _logger.LogInformation(
            "Enqueued message for OutgoingMessageId: {OutgoingMessageId} for ActorNumber: {ActorNumber} for Received Event id: {EventId}",
            messageToEnqueue.Id,
            messageToEnqueue.Receiver.Number.Value,
            messageToEnqueue.EventId);

        return messageToEnqueue.Id;
    }

    private Bundle CreateBundle(OutgoingMessage messageToEnqueue, ActorMessageQueueId actorMessageQueueId)
    {
        var newBundle = CreateBundle(
            actorMessageQueueId,
            BusinessReason.FromName(messageToEnqueue.BusinessReason),
            messageToEnqueue.DocumentType,
            _systemDateTimeProvider.Now(),
            messageToEnqueue.RelatedToMessageId);
        _bundleRepository.Add(newBundle);

        return newBundle;
    }

    private async Task<ActorMessageQueueId> GetMessageQueueIdForReceiverAsync(Receiver receiver)
    {
        var actorMessageQueueId = await _actorMessageQueueRepository.ActorMessageQueueIdForAsync(
            receiver.Number,
            receiver.ActorRole).ConfigureAwait(false);

        if (actorMessageQueueId == null)
        {
            _logger.LogInformation("Creating new message queue for Actor: {ActorNumber}, MarketRole: {MarketRole}", receiver.Number.Value, receiver.ActorRole.Name);
            var actorMessageQueueToCreate = ActorMessageQueue.CreateFor(receiver);
            _actorMessageQueueRepository.Add(actorMessageQueueToCreate);
            actorMessageQueueId = actorMessageQueueToCreate.Id;
        }

        return actorMessageQueueId;
    }

    private Bundle CreateBundle(ActorMessageQueueId actorMessageQueueId, BusinessReason businessReason, DocumentType messageType, Instant created, MessageId? relatedToMessageId = null)
    {
        return new Bundle(actorMessageQueueId, businessReason, messageType, 1, created, relatedToMessageId);
    }
}
