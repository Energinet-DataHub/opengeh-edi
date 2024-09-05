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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Dequeue is used by the actor to acknowledge a message was successfully received.
/// The message is then removed from their queue. And next message is ready to be peeked.
/// </summary>
public class DequeueMessage
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IBundleRepository _bundleRepository;
    private readonly ILogger<DequeueMessage> _logger;

    public DequeueMessage(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IBundleRepository bundleRepository,
        ILogger<DequeueMessage> logger)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _bundleRepository = bundleRepository;
        _logger = logger;
    }

    public async Task<DequeueRequestResultDto> DequeueAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack)
        {
            request = request with { ActorRole = request.ActorRole.ForActorMessageQueue(), };
        }

        MessageId messageId;
        try
        {
            messageId = MessageId.Create(request.MessageId);
        }
        catch (InvalidMessageIdException)
        {
            _logger.LogWarning("Invalid message id: {MessageId}", request.MessageId);
            return new DequeueRequestResultDto(false);
        }

        bool dequeueResult = false;
        var bundle = await _bundleRepository.GetBundleAsync(messageId, cancellationToken).ConfigureAwait(false);
        if (bundle == null)
            return new DequeueRequestResultDto(dequeueResult);

        dequeueResult = bundle.TryDequeue();

        _logger.LogInformation("Dequeue request result: {Successful} for messageId: {BundleId}", dequeueResult, messageId.Value);
        return new DequeueRequestResultDto(dequeueResult);
    }
}
