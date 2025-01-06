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

using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

public class EdiFileStorageDriver(string connectionString)
{
    private readonly BlobServiceClient _blobServiceClient = new(
        new Uri(connectionString),
        new DefaultAzureCredential());

    public async Task DeleteOutgoingMessagesIfExistsAsync(IList<string> fileStorageReferences, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient("outgoing");
        var blobBatchClient = _blobServiceClient.GetBlobBatchClient();
        var blobUris = fileStorageReferences
            .Select(reference => container.GetBlobClient(reference).Uri)
            .ToList();

        // Each batch request supports a maximum of 256 blobs.
        var take = 256;
        var skip = 0;
        while (true)
        {
            var batch = blobUris.Skip(skip).Take(take).ToList();
            skip += take;
            if (batch.Count == 0)
                break;

            try
            {
                await blobBatchClient.DeleteBlobsAsync(batch, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException e) when (e.InnerExceptions.Any(x => x is RequestFailedException && x.Message.Contains("BlobNotFound")))
            {
                // One or more Blobs did not exist, no need to delete.
                return;
            }
        }
    }
}
