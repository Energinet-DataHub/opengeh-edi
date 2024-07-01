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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain;

public class EnqueueMessageService(IActorMessageQueueRepository actorMessageQueueRepository, ILogger<EnqueueMessageService> logger)
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository = actorMessageQueueRepository;
    private readonly ILogger<EnqueueMessageService> _logger = logger;

    public async Task EnqueueAsync(OutgoingMessage outgoingMessage, Instant timeStamp)
    {
        ArgumentNullException.ThrowIfNull(outgoingMessage);

        var actorMessageQueueId =
            await GetMessageQueueIdForReceiverAsync(outgoingMessage.GetActorMessageQueueMetadata())
                .ConfigureAwait(false);

        var currentBundle = CreateBundle(
            actorMessageQueueId,
            BusinessReason.FromName(outgoingMessage.BusinessReason),
            outgoingMessage.DocumentType,
            timeStamp,
            outgoingMessage.RelatedToMessageId);

        currentBundle.Add(outgoingMessage);
    }

    private Bundle CreateBundle(ActorMessageQueueId actorMessageQueueId, BusinessReason businessReason, DocumentType messageType, Instant created, MessageId? relatedToMessageId = null)
    {
        var bundle = new Bundle(actorMessageQueueId, businessReason, messageType, 1, created, relatedToMessageId);
        return bundle;
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
}
