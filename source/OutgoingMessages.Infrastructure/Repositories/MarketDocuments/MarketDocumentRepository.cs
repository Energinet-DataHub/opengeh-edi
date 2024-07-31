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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.MarketDocuments;

public class MarketDocumentRepository : IMarketDocumentRepository
{
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly IFileStorageClient _fileStorageClient;

    public MarketDocumentRepository(ActorMessageQueueContext actorMessageQueueContext, IFileStorageClient fileStorageClient)
    {
        _actorMessageQueueContext = actorMessageQueueContext;
        _fileStorageClient = fileStorageClient;
    }

    public async Task<MarketDocument?> GetAsync(BundleId bundleId)
    {
        var marketDocument = await _actorMessageQueueContext.MarketDocuments.FirstOrDefaultAsync(x => x.BundleId == bundleId).ConfigureAwait(false);

        if (marketDocument != null)
        {
            var fileStorageFile = await _fileStorageClient.DownloadAsync(marketDocument.FileStorageReference).ConfigureAwait(false);

            var marketDocumentStream = new MarketDocumentStream(fileStorageFile);
            marketDocument.SetMarketDocumentStream(marketDocumentStream);
        }

        return marketDocument;
    }

    public void Add(MarketDocument marketDocument)
    {
        _actorMessageQueueContext.Add(marketDocument);
    }

    public async Task DeleteMarketDocumentsIfExistsAsync(IReadOnlyCollection<BundleId> bundleMessageIds)
    {
        var marketDocuments = await _actorMessageQueueContext.MarketDocuments.Where(x => bundleMessageIds.Contains(x.BundleId))
            .ToListAsync()
            .ConfigureAwait(false);
        _actorMessageQueueContext.RemoveRange(marketDocuments);
    }
}
