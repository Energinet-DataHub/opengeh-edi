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
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;

public class DataLakeFileStorageClient : IFileStorageClient
{
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly ILogger<DataLakeFileStorageClient> _logger;
    private readonly BlobServiceClient _blobServiceClientObsoleted;
    private readonly BlobServiceClient _blobServiceClient;

    public DataLakeFileStorageClient(
        IAzureClientFactory<BlobServiceClient> clientFactory,
        IOptions<BlobServiceClientConnectionOptions> options,
        IFeatureFlagManager featureFlagManager,
        ILogger<DataLakeFileStorageClient> logger)
    {
        _featureFlagManager = featureFlagManager;
        _logger = logger;
        _blobServiceClientObsoleted = clientFactory.CreateClient(options.Value.ClientNameObsoleted);
        _blobServiceClient = clientFactory.CreateClient(options.Value.ClientName);
    }

    public async Task UploadAsync(FileStorageReference reference, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(reference.Category.Value);
        if (!await _featureFlagManager.UseStandardBlobServiceClientAsync().ConfigureAwait(false))
        {
            container = _blobServiceClientObsoleted.GetBlobContainerClient(reference.Category.Value);
        }

        stream.Position = 0; // Make sure we read the entire stream
        await container.UploadBlobAsync(reference.Path, stream).ConfigureAwait(false);
        stream.Position = 0; // Reset stream position so it can be read again
    }

    public async Task UploadAsync(FileStorageReference reference, string content)
    {
        var memoryStream = new MemoryStream();

        // Is disposed when the MemoryStream is disposed
#pragma warning disable CA2000
        var streamWriter = new StreamWriter(memoryStream);
#pragma warning restore CA2000

        await streamWriter.WriteAsync(content).ConfigureAwait(false);
        await streamWriter.FlushAsync().ConfigureAwait(false);

        await UploadAsync(reference, memoryStream).ConfigureAwait(false);
    }

    public async Task<FileStorageFile> DownloadAsync(FileStorageReference reference, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(reference.Category.Value);
        var blob = container.GetBlobClient(reference.Path);

        var blobExists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (blobExists == null || blobExists is { Value: false })
        {
            // This logging is only temporary while we verify data migration is successful.
            _logger.LogInformation("Blob does not exist in the new storage account, trying the obsoleted storage account. Category {Category}, Path {Path}", reference.Category.Value, reference.Path);
            var containerObsoleted = _blobServiceClientObsoleted.GetBlobContainerClient(reference.Category.Value);
            blob = containerObsoleted.GetBlobClient(reference.Path);
        }

        // OpenReadAsync() returns a stream for the file, and the file is downloaded the first time the stream is read
        var downloadStream = await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return new FileStorageFile(downloadStream);
    }

    public async Task DeleteIfExistsAsync(IReadOnlyList<FileStorageReference> fileStorageReferences, FileStorageCategory fileStorageCategory, CancellationToken cancellationToken = default)
    {
        await DeleteFromClientAsync(_blobServiceClientObsoleted, fileStorageReferences, fileStorageCategory, cancellationToken).ConfigureAwait(false);
        await DeleteFromClientAsync(_blobServiceClient, fileStorageReferences, fileStorageCategory, cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteFromClientAsync(BlobServiceClient client, IReadOnlyList<FileStorageReference> fileStorageReferences, FileStorageCategory fileStorageCategory, CancellationToken cancellationToken)
    {
        var container = client.GetBlobContainerClient(fileStorageCategory.Value);
        var blobBatchClient = client.GetBlobBatchClient();

        var blobUris = fileStorageReferences
            .Where(x => x.Category == fileStorageCategory)
            .Select(reference => container.GetBlobClient(reference.Path).Uri)
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
