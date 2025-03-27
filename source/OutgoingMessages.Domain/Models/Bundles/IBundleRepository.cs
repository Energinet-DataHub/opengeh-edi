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
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

/// <summary>
/// The repository for bundles
/// </summary>
public interface IBundleRepository
{
    /// <summary>
    /// Add a new Bundle
    /// </summary>
    void Add(Bundle bundle);

    void Add(IList<Bundle> bundles);

    /// <summary>
    ///  Get dequeued bundles older than a specific time.
    /// </summary>
    /// <param name="olderThan"></param>
    /// <param name="take"></param>
    /// <param name="cancellationToken"></param>
    Task<IReadOnlyCollection<Bundle>> GetDequeuedBundlesOlderThanAsync(Instant olderThan, int take, CancellationToken cancellationToken);

    /// <summary>
    ///  Delete bundles.
    /// </summary>
    /// <param name="bundles"></param>
    void Delete(IReadOnlyCollection<Bundle> bundles);

    /// <summary>
    /// Get bundle
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="cancellationToken"></param>
    Task<Bundle?> GetBundleAsync(MessageId messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Get open bundle for a given document type and actor message queue.
    /// </summary>
    Task<Bundle?> GetOpenBundleAsync(
        DocumentType documentType,
        BusinessReason businessReason,
        ActorMessageQueueId actorMessageQueueId,
        MessageId? relatedToMessageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the next bundle to peek for an ActorMessageQueue and a category.
    /// </summary>
    /// <returns>The oldest closed bundle that hasn't been dequeued yet for given inputs.</returns>
    Task<Bundle?> GetNextBundleToPeekAsync(ActorMessageQueueId actorMessageQueueId, MessageCategory messageCategory, CancellationToken cancellationToken);
}
