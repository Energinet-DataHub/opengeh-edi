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
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class MessageEnqueuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<MessageEnqueuer> _logger;
    private readonly MessageDelegator _messageDelegator;
    private readonly IFeatureFlagManager _featureFlagManager;

    public MessageEnqueuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<MessageEnqueuer> logger,
        MessageDelegator messageDelegator,
        IFeatureFlagManager featureFlagManager)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
        _logger = logger;
        _messageDelegator = messageDelegator;
        _featureFlagManager = featureFlagManager;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        if (await _featureFlagManager.UseMessageDelegationAsync().ConfigureAwait(false))
        {
            messageToEnqueue = await _messageDelegator.DelegateAsync(messageToEnqueue, cancellationToken)
                .ConfigureAwait(false);
        }

        var addToRepositoryTask = _outgoingMessageRepository.AddAsync(messageToEnqueue);
        var addToActorMessageQueueTask = AddToActorMessageQueueAsync(messageToEnqueue);

        await Task.WhenAll(addToRepositoryTask, addToActorMessageQueueTask).ConfigureAwait(false);
        _logger.LogInformation("Message enqueued: {Message} for Actor: {ActorNumber}", messageToEnqueue.Id, messageToEnqueue.Receiver.Number.Value);

        return messageToEnqueue.Id;
    }

    private async Task AddToActorMessageQueueAsync(OutgoingMessage outgoingMessage)
    {
        var actorMessageQueue = await GetMessageQueueForReceiverAsync(outgoingMessage.GetActorMessageQueueMetadata()).ConfigureAwait(false);
        actorMessageQueue.Enqueue(outgoingMessage, _systemDateTimeProvider.Now());
    }

    private async Task<ActorMessageQueue> GetMessageQueueForReceiverAsync(Receiver receiver)
    {
        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            receiver.Number,
            receiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            _logger.LogInformation("Creating new message queue for Actor: {ActorNumber}, MarketRole: {MarketRole}", receiver.Number.Value, receiver.ActorRole.Name);
            messageQueue = ActorMessageQueue.CreateFor(receiver);
            _actorMessageQueueRepository.Add(messageQueue);
        }

        return messageQueue;
    }
}
