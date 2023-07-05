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
using System.Threading.Tasks;
using Domain.Actors;
using Domain.OutgoingMessages.Peek;
using Domain.OutgoingMessages.Queueing;
using Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.OutgoingMessages.Queueing;

public class ActorMessageQueueRepository : IActorMessageQueueRepository
{
    private readonly B2BContext _b2BContext;

    public ActorMessageQueueRepository(B2BContext b2BContext)
    {
        _b2BContext = b2BContext;
    }

    public async Task<ActorMessageQueue> ActorMessageQueueForAsync(ActorNumber actorNumber, MessageCategory messageCategory)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        var sql = $"SELECT * FROM [dbo].[ActorMessageQueues] m join [dbo].[Bundles] b on m.Id = b.ActorMessageQueueId " +
                  $"WHERE m.ActorNumber = {actorNumber.Value} AND b.IsDequeued = 0 AND b.DocumentTypeInBundle in ('NotifyAggregatedMeasureData')";
        var actorMessageQueue = await _b2BContext.ActorMessageQueues.FromSqlRaw(sql, actorNumber.Value).FirstOrDefaultAsync().ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(actorMessageQueue);

        return actorMessageQueue;
    }
}
