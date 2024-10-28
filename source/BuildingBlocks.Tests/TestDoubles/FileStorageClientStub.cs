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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;

namespace Energinet.DataHub.BuildingBlocks.Tests.TestDoubles;

/// <summary>
/// A IFileStorageClient that avoids uploading a file to Azurite,
/// used to be able to check a duplicate id database constraint
/// (the normal implementation throwing a Azure.RequestFailedException when trying to add duplicate id in file storage)
/// </summary>
public class FileStorageClientStub : IFileStorageClient
{
    public Task UploadAsync(FileStorageReference reference, Stream stream)
    {
        return Task.CompletedTask;
    }

    public Task UploadAsync(FileStorageReference reference, string content)
    {
        return Task.CompletedTask;
    }

    public Task<FileStorageFile> DownloadAsync(FileStorageReference reference, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteIfExistsAsync(IReadOnlyList<FileStorageReference> fileStorageReferences, FileStorageCategory fileStorageCategory, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
