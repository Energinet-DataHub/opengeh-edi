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
using Messaging.Application.OutgoingMessages.Dequeue;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

/// <summary>
/// Repository of ready messages
/// </summary>
public interface IReadyMessages
{
    /// <summary>
    /// Adds a ready message to repository
    /// </summary>
    /// <param name="readyMessage"></param>
    /// <returns>void</returns>
    Task<bool> TryAddAsync(ReadyMessage readyMessage);

    /// <summary>
    /// Dequeue ready message
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns>DequeueResult</returns>
    Task<DequeueResult> DequeueAsync(Guid messageId);

    /// <summary>
    /// Retrieve a ready message from repository
    /// </summary>
    /// <param name="category"></param>
    /// <param name="receiverNumber"></param>
    /// <returns><see cref="ReadyMessage"/></returns>
    Task<ReadyMessage?> GetAsync(MessageCategory category, ActorNumber receiverNumber);
}
