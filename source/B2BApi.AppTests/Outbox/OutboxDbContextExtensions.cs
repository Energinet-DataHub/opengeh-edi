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

using Energinet.DataHub.Core.Outbox.Domain;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Outbox;

public static class OutboxDbContextExtensions
{
    public static async Task<(bool Success, OutboxMessage? OutboxMessage)> WaitForOutboxMessageToBeProcessedAsync(
        this OutboxContext outboxContext,
        OutboxMessageId id,
        int timeoutInSeconds = 30)
    {
        OutboxMessage? outboxMessage = null;
        var result = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                outboxMessage = await outboxContext.Outbox
                    .AsNoTracking() // Needed to avoid caching the result
                    .SingleOrDefaultAsync(m => m.Id == id)
                    .ConfigureAwait(false);

                return outboxMessage?.PublishedAt != null || outboxMessage?.FailedAt != null;
            },
            TimeSpan.FromSeconds(timeoutInSeconds));

        return (result, outboxMessage);
    }
}
