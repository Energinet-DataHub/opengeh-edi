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

using Energinet.DataHub.EDI.Outbox.Domain;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Outbox.Infrastructure;

public class OutboxRepository(OutboxContext outboxContext) : IOutboxRepository
{
    private readonly OutboxContext _outboxContext = outboxContext;

    public void Add(OutboxMessage outboxMessage)
    {
        _outboxContext.OutboxMessages.Add(outboxMessage);
    }

    public async Task<IReadOnlyCollection<OutboxMessageId>> GetUnprocessedOutboxMessageIdsAsync()
    {
        var outboxMessageIds = await _outboxContext.OutboxMessages
            .Where(om => om.PublishedAt == null)
            .Select(om => om.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        return outboxMessageIds;
    }

    public Task<OutboxMessage> GetAsync(OutboxMessageId outboxMessageId)
    {
        return _outboxContext.OutboxMessages.SingleAsync(om => om.Id == outboxMessageId);
    }
}
