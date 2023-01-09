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

using System.Collections.Generic;
using System.Threading.Tasks;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages;

/// <summary>
/// Interface to get enqueued messages
/// </summary>
public interface IEnqueuedMessages
{
    /// <summary>
    /// Get enqueued messages
    /// </summary>
    /// <param name="actorNumber">Actor number of requesting actor</param>
    /// /// /// <param name="messageCategory">The category of messages to include in message</param>
    /// <returns>List of enqueued messages</returns>
    Task<MessageBundle> GetByAsync(ActorNumber actorNumber, MessageCategory messageCategory);

    /// <summary>
    /// Get the number of messages available for an actor
    /// </summary>
    /// <param name="actorNumber"></param>
    /// <returns>Number of available messages</returns>
    Task<int> GetAvailableMessageCountAsync(ActorNumber actorNumber);
}
