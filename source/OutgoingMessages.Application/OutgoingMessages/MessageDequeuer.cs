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

using System;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class MessageDequeuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly ILogger<MessageDequeuer> _logger;

    public MessageDequeuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        ILogger<MessageDequeuer> logger)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _logger = logger;
    }

    public async Task<DequeueRequestResultDto> DequeueAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Guid.TryParse(request.MessageId, out var messageId) == false)
        {
            _logger.LogWarning("Invalid message id: {MessageId}", request.MessageId);
            return new DequeueRequestResultDto(false);
        }

        var bundleId = BundleId.Create(messageId);
        var actorQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.ActorRole).ConfigureAwait(false);
        if (actorQueue == null)
        {
            _logger.LogWarning("Actor queue not found for actor number: {ActorNumber} and market role: {MarketRole}", request.ActorNumber, request.ActorRole);
            return new DequeueRequestResultDto(false);
        }

        var successful = actorQueue.Dequeue(bundleId);
        _logger.LogInformation("Dequeue request result: {Successful} for bundleId: {BundleId}", successful, bundleId);
        return new DequeueRequestResultDto(successful);
    }
}
