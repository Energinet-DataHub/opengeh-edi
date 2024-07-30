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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;

public class DequeuedBundlesRetention : IDataRetention
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketDocumentRepository _marketDocumentRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly IBundleRepository _bundleRepository;
    private readonly ILogger<DequeuedBundlesRetention> _logger;

    public DequeuedBundlesRetention(
        ISystemDateTimeProvider systemDateTimeProvider,
        IMarketDocumentRepository marketDocumentRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ActorMessageQueueContext actorMessageQueueContext,
        IBundleRepository bundleRepository,
        ILogger<DequeuedBundlesRetention> logger)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _marketDocumentRepository = marketDocumentRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _actorMessageQueueContext = actorMessageQueueContext;
        _bundleRepository = bundleRepository;
        _logger = logger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var monthAgo = _systemDateTimeProvider.Now().Plus(-Duration.FromDays(30));
            var dequeuedBundles = await _bundleRepository.GetDequeuedBundlesOlderThanAsync(monthAgo, 500).ConfigureAwait(false);

            if (dequeuedBundles.Count == 0)
            {
                break;
            }

            foreach (var bundleId in dequeuedBundles)
            {
                        try
                        {
                            ArgumentNullException.ThrowIfNull(bundleId);
                            await _marketDocumentRepository.DeleteMarketDocumentIfExistsAsync(bundleId.Id).ConfigureAwait(false);
                            await _outgoingMessageRepository.DeleteOutgoingMessageIfExistsAsync(bundleId.Id).ConfigureAwait(false);
                            _bundleRepository.Delete(bundleId);
                            await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to remove bundle with id {BundleId}", bundleId!.Id);
                        }
            }
        }
    }
}
