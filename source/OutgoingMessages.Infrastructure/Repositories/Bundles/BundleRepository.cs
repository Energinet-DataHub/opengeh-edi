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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.Bundles;

public class BundleRepository(
    ActorMessageQueueContext dbContext,
    IClock clock,
    IOptions<BundlingOptions> options)
        : IBundleRepository
{
    private readonly ActorMessageQueueContext _dbContext = dbContext;
    private readonly IClock _clock = clock;
    private readonly BundlingOptions _options = options.Value;

    public void Add(Bundle bundle)
    {
        _dbContext.Bundles.Add(bundle);
    }

    public void Delete(IReadOnlyCollection<Bundle> bundles)
    {
        _dbContext.Bundles.RemoveRange(bundles);
    }

    public async Task<IReadOnlyCollection<Bundle>> GetDequeuedBundlesOlderThanAsync(Instant olderThan, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bundles.Where(x => x.DequeuedAt < olderThan)
            .Take(take)
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Bundle?> GetBundleAsync(MessageId messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Bundles.FirstOrDefaultAsync(x => x.MessageId == messageId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<Bundle?> GetOpenBundleAsync(
        DocumentType documentType,
        BusinessReason businessReason,
        ActorMessageQueueId actorMessageQueueId,
        MessageId? relatedToMessageId,
        CancellationToken cancellationToken)
    {
        // TODO: Can we assume there will always only be one bundle (.Single() instead of .First())?
        return _dbContext.Bundles
            .Where(
                b =>
                    b.ActorMessageQueueId == actorMessageQueueId &&
                    b.DocumentTypeInBundle == documentType &&
                    b.BusinessReason == businessReason &&
                    b.RelatedToMessageId == relatedToMessageId &&
                    b.ClosedAt == null)
            .OrderByDescending(b => b.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Bundle?> GetOldestBundleAsync(
        ActorMessageQueueId actorMessageQueueId,
        MessageCategory messageCategory,
        CancellationToken cancellationToken = default)
    {
        // Get oldest bundle that is:
        // - In the given actor message queue
        // - Not dequeued
        // - Already closed or is created more than "BundleDurationInMinutes" minutes ago
        var closeBundlesCreatedBefore = _clock
            .GetCurrentInstant()
            .Minus(Duration.FromMinutes(_options.BundleDurationInMinutes));

        var query = _dbContext.Bundles.Where(
            b =>
                b.ActorMessageQueueId == actorMessageQueueId &&
                b.DequeuedAt == null &&
                (b.ClosedAt != null || b.Created <= closeBundlesCreatedBefore));

        if (messageCategory != MessageCategory.None)
            query = query.Where(b => b.MessageCategory == messageCategory);

        var oldestBundle = await query
            .OrderBy(b => b.Created)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return oldestBundle;
    }
}
