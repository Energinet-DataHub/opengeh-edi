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

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Files.DataLake;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

public class DataLakeFileStorageClient : IFileStorageClient
{
    private readonly BlobServiceClient _blobStorageClient;
    // private readonly DataLakeServiceClient _dataLakeServiceClient;

    public DataLakeFileStorageClient(IOptions<AzureDataLakeConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _blobStorageClient = new BlobServiceClient(options.Value.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING);
    }

    public async Task UploadAsync(string rootFolder, string reference, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var container = _blobStorageClient.GetBlobContainerClient(rootFolder);

        var containerExists = await container.ExistsAsync().ConfigureAwait(false);

        if (!containerExists)
            await container.CreateAsync().ConfigureAwait(false);

        stream.Position = 0;
        await container.UploadBlobAsync(reference, stream).ConfigureAwait(false);
    }

    public async Task<Stream> DownloadAsync(string rootFolder, string reference)
    {
        var container = _blobStorageClient.GetBlobContainerClient(rootFolder);

        var blob = container.GetBlobClient(reference);

        var stream = new MemoryStream();
        await blob.DownloadToAsync(stream).ConfigureAwait(false);

        return stream;
    }
}
