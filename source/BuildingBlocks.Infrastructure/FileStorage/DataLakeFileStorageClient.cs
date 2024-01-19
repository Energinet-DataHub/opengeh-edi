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
using Azure.Storage.Blobs;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

public class DataLakeFileStorageClient : IFileStorageClient
{
    private readonly BlobServiceClient _blobServiceClient;

    public DataLakeFileStorageClient(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task UploadAsync(string rootFolder, FileStorageReference reference, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(rootFolder);

        var containerExists = await container.ExistsAsync().ConfigureAwait(false);

        if (!containerExists)
            await container.CreateAsync().ConfigureAwait(false);

        stream.Position = 0; // Make sure we read the entire stream
        await container.UploadBlobAsync(reference.Value, stream).ConfigureAwait(false);
    }

    public async Task<Stream> DownloadAsync(string rootFolder, FileStorageReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(rootFolder);

        var blob = container.GetBlobClient(reference.Value);

        var stream = new MemoryStream();
        await blob.DownloadToAsync(stream).ConfigureAwait(false);

        stream.Position = 0; // Make sure stream is ready to be read
        return stream;
    }
}
