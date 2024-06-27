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
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

public class DataLakeFileStorageClient : IFileStorageClient
{
    private readonly BlobServiceClient _blobServiceClient;

    public DataLakeFileStorageClient(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task UploadAsync(FileStorageReference reference, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(reference.Category.Value);

        var containerExists = await container.ExistsAsync().ConfigureAwait(false);

        if (!containerExists)
            await container.CreateAsync().ConfigureAwait(false);

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

    public async Task<FileStorageFile> DownloadAsync(FileStorageReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        var container = _blobServiceClient.GetBlobContainerClient(reference.Category.Value);

        var blob = container.GetBlobClient(reference.Path);

        // OpenReadAsync() returns a stream for the file, and the file is downloaded the first time the stream is read
        var downloadStream = await blob.OpenReadAsync().ConfigureAwait(false);

        return new FileStorageFile(downloadStream);
    }

    public async Task DeleteIfExistsAsync(FileStorageReference reference)
    {
        var container = _blobServiceClient.GetBlobContainerClient(reference.Category.Value);

        var blob = container.GetBlobClient(reference.Path);

        await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots).ConfigureAwait(false);
    }
}
