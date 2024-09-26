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

using Azure.Storage.Blobs;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;

public class MasterDataFixture : IDisposable, IAsyncLifetime
{
    private bool _disposed;

    public AzuriteManager AzuriteManager { get; } = new(true);

    public EdiDatabaseManager DatabaseManager { get; set; } = new();

    public ServiceProvider ServiceProvider { get; private set; } = null!;

    public void CleanupDatabase()
    {
        DatabaseManager.CleanupDatabase();
    }

    public async Task InitializeAsync()
    {
        await DatabaseManager.CreateDatabaseAsync();
        AzuriteManager.StartAzurite();
    }

    public async Task DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            DatabaseManager.DeleteDatabase();
            AzuriteManager.Dispose();
        }

        _disposed = true;
    }

    private void CreateRequiredContainers()
    {
        List<FileStorageCategory> containerCategories = [
            FileStorageCategory.ArchivedMessage(),
        ];

        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);
        foreach (var fileStorageCategory in containerCategories)
        {
            var container = blobServiceClient.GetBlobContainerClient(fileStorageCategory.Value);
            var containerExists = container.Exists();

            if (!containerExists)
                container.Create();
        }
    }
}
