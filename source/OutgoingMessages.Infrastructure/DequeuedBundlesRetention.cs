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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

public class DequeuedBundlesRetention : IDataRetention
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IMarketDocumentRepository _marketDocumentRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly ILogger<DequeuedBundlesRetention> _logger;

    public DequeuedBundlesRetention(
        ISystemDateTimeProvider systemDateTimeProvider,
        IActorMessageQueueRepository actorMessageQueueRepository,
        IMarketDocumentRepository marketDocumentRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ActorMessageQueueContext actorMessageQueueContext,
        ILogger<DequeuedBundlesRetention> logger)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _marketDocumentRepository = marketDocumentRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueContext = actorMessageQueueContext;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        const int incrementer = 10;
        var skip = 0;
        var take = 10;
        var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(30));
        while (true)
        {
            var actorMessageQueues =
                await _actorMessageQueueRepository.GetActorMessageQueuesAsync(skip, take).ConfigureAwait(false);

            if (actorMessageQueues.Count == 0)
            {
                break;
            }

            foreach (var actorMessageQueue in actorMessageQueues)
            {
                var dequeuedBundles = actorMessageQueue.GetDequeuedBundles();
                foreach (var bundle in dequeuedBundles)
                {
                    if (bundle.DequeuedAt < monthAgo)
                    {
                        try
                        {
                            await _marketDocumentRepository.DeleteMarketIfExistsDocumentAsync(bundle.Id).ConfigureAwait(false);
                            await _outgoingMessageRepository.DeleteOutgoingMessageIfExistsAsync(bundle.Id).ConfigureAwait(false);
                            actorMessageQueue.RemoveBundle(bundle);
                            await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to remove bundle with id {BundleId}", bundle.Id);
                        }
                    }
                }
            }

            skip += incrementer;
            take += incrementer;
        }
    }
}
