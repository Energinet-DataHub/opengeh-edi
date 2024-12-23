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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;

/// <summary>
/// The repository for the actor message queue.
/// </summary>
public interface IActorMessageQueueRepository
{
    /// <summary>
    /// Get the actor queue for the given actor number and role.
    /// </summary>
    Task<ActorMessageQueue?> ActorMessageQueueForAsync(ActorNumber actorNumber, ActorRole actorRole, CancellationToken cancellationToken);

    /// <summary>
    /// Get the actor queue for the given actor number and role.
    /// </summary>
    Task<ActorMessageQueueId?> ActorMessageQueueIdForAsync(ActorNumber actorNumber, ActorRole actorRole, CancellationToken cancellationToken);

    /// <summary>
    /// Add a new actor queue.
    /// </summary>
    void Add(ActorMessageQueue actorMessageQueue);
}
