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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Enqueue is used by EDI to deliver a message to an appropriate actors queue.
/// </summary>
public class EnqueueMessage
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<EnqueueMessage> _logger;
    private readonly DelegateMessage _delegateMessage;
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly EnqueueMessageService _enqueueMessageService;

    public EnqueueMessage(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<EnqueueMessage> logger,
        DelegateMessage delegateMessage,
        IFeatureFlagManager featureFlagManager,
        EnqueueMessageService enqueueMessageService)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
        _logger = logger;
        _delegateMessage = delegateMessage;
        _featureFlagManager = featureFlagManager;
        _enqueueMessageService = enqueueMessageService;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        if (await _featureFlagManager.UseMessageDelegationAsync().ConfigureAwait(false))
        {
            messageToEnqueue = await _delegateMessage.DelegateAsync(messageToEnqueue, cancellationToken)
                .ConfigureAwait(false);
        }

        var messageExists = await _outgoingMessageRepository.GetAsync(messageToEnqueue.Receiver, messageToEnqueue.ExternalId).ConfigureAwait(false);
        if (messageExists != null)
            return messageExists.Id;

        await _enqueueMessageService.EnqueueAsync(messageToEnqueue, _systemDateTimeProvider.Now())
            .ConfigureAwait(false);

        // Add to outgoing message repository (and upload to file storage) after adding actor message queue,
        // to minimize the cases where a message is uploaded to file storage but adding actor message queue fails
        await _outgoingMessageRepository.AddAsync(messageToEnqueue).ConfigureAwait(false);

        _logger.LogInformation(
            "Enqueued message for OutgoingMessageId: {OutgoingMessageId} for ActorNumber: {ActorNumber} for Received Event id: {EventId}",
            messageToEnqueue.Id,
            messageToEnqueue.Receiver.Number.Value,
            messageToEnqueue.EventId);

        return messageToEnqueue.Id;
    }
}
