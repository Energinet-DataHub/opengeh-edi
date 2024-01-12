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
using Azure.Storage.Files.DataLake;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

public class DataLakeFileStorageClient : IFileStorageClient
{
    private readonly DataLakeFileSystemClient _dataLakeFileSystemClient;

    public DataLakeFileStorageClient(IOptions<AzureDataLakeConnectionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var credentials = new StorageSharedKeyCredential(options.Value.AZURE_STORAGE_ACCOUNT_NAME, options.Value.AZURE_STORAGE_ACCOUNT_KEY);

        // var dataLakeServiceClient = new DataLakeServiceClient(new Uri(options.Value.AZURE_DATA_LAKE_URI), credentials);
        var dataLakeServiceClient = new DataLakeServiceClient(options.Value.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING);
        _dataLakeFileSystemClient = dataLakeServiceClient.GetFileSystemClient(options.Value.AZURE_DATA_LAKE_FILESYSTEM_NAME);
    }

    public async Task UploadAsync(string reference, Stream stream)
    {
        await _dataLakeFileSystemClient.CreateIfNotExistsAsync().ConfigureAwait(false);

        var fileClient = _dataLakeFileSystemClient.GetFileClient(reference);

        await fileClient.UploadAsync(stream, overwrite: false).ConfigureAwait(false);
    }

    public async Task<Stream> DownloadAsync(string reference)
    {
        await _dataLakeFileSystemClient.CreateIfNotExistsAsync().ConfigureAwait(false);

        var fileClient = _dataLakeFileSystemClient.GetFileClient(reference);

        var destinationStream = new MemoryStream();
        await fileClient.ReadToAsync(destinationStream).ConfigureAwait(false);

        return destinationStream;
    }
}
