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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

namespace Energinet.DataHub.EDI.Outbox.Interfaces;

/// <summary>
/// An outbox client is responsible for persisting outbox entries. The outbox should be used for persisting
/// emails, events or http requests that needs to be sent to external services, and guarantees the message is sent atleast once.
/// <remarks>
/// An example of the outbox pattern documentation can be found at: https://www.kamilgrzybek.com/blog/posts/the-outbox-pattern
/// referenced by Microsoft at https://learn.microsoft.com/en-us/dotnet/architecture/microservices/architect-microservice-container-applications/asynchronous-message-based-communication
/// </remarks>
/// </summary>
public interface IOutboxClient
{
    /// <summary>
    /// Adds an outbox messages to the storage, without commiting the transaction.
    /// <remarks>Use <see cref="IUnitOfWork"/> to commit the transaction when needed.</remarks>
    /// </summary>
    public Task CreateWithoutCommitAsync<TPayload>(IOutboxMessage<TPayload> message);
}
