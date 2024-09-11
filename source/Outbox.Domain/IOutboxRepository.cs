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

namespace Energinet.DataHub.EDI.Outbox.Domain;

/// <summary>
/// A repository of outbox messages, used to implement an outbox pattern.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Add outbox message to the repository.
    /// </summary>
    void Add(OutboxMessage outboxMessage);

    /// <summary>
    /// Get all unprocessed outbox messages.
    /// </summary>
    public Task<IReadOnlyCollection<OutboxMessageId>> GetUnprocessedOutboxMessageIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get outbox message by id.
    /// </summary>
    Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId, CancellationToken cancellationToken);
}
