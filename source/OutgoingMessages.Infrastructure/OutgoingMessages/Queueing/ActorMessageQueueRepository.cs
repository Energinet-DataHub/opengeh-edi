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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;

public class ActorMessageQueueRepository : IActorMessageQueueRepository
{
    private readonly ActorMessageQueueContext _actorMessageQueueContext;

    public ActorMessageQueueRepository(ActorMessageQueueContext actorMessageQueueContext)
    {
        _actorMessageQueueContext = actorMessageQueueContext;
    }

    public async Task<ActorMessageQueue?> ActorMessageQueueForAsync(ActorNumber actorNumber, ActorRole actorRole)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        ArgumentNullException.ThrowIfNull(actorRole);

        var actorMessageQueue = await _actorMessageQueueContext.ActorMessageQueues.FirstOrDefaultAsync(queue =>
            queue.Receiver.Number.Equals(actorNumber) && queue.Receiver.ActorRole.Equals(actorRole)).ConfigureAwait(false);

        if (actorMessageQueue is null)
        {
            actorMessageQueue = _actorMessageQueueContext.ActorMessageQueues.Local.FirstOrDefault(queue =>
                queue.Receiver.Number.Equals(actorNumber) && queue.Receiver.ActorRole.Equals(actorRole));
        }

        return actorMessageQueue;
    }

    public void Add(ActorMessageQueue actorMessageQueue)
    {
        _actorMessageQueueContext.ActorMessageQueues.Add(actorMessageQueue);
    }
}
