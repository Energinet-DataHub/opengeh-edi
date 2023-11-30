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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using MediatR;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class MessageDequeuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;

    public MessageDequeuer(IActorMessageQueueRepository actorMessageQueueRepository)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
    }

    public async Task<DequeueRequestResultDto> DequeueAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Guid.TryParse(request.MessageId, out var messageId) == false)
        {
            return new DequeueRequestResultDto(false);
        }

        var bundleId = BundleId.Create(messageId);
        var actorQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(request.ActorNumber, request.MarketRole).ConfigureAwait(false);
        if (actorQueue == null)
        {
            return new DequeueRequestResultDto(false);
        }

        var successful = actorQueue.Dequeue(bundleId);

        return new DequeueRequestResultDto(successful);
    }
}
