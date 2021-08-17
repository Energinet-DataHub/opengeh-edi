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

using System.Threading.Tasks;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Messaging.Idempotency
{
    /// <summary>
    /// Service for registering coming messages in order to handle message idempotency.
    /// Messages can business process requests and integration events reveived from other bounded contexts.
    /// </summary>
    public interface IIncomingMessageRegistry
    {
        /// <summary>
        /// Register an message
        /// </summary>
        /// <param name="messageId">Unique id of incoming message</param>
        /// <param name="messageType">Message type</param>
        /// <returns><see cref="Task"/></returns>
        Task RegisterMessageAsync(string messageId, string messageType);
    }
}
