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

using ArchivedMessages.IntegrationTests.Fixture.Database;
using Azure.Storage.Blobs;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;

public class ArchivedMessagesFixture : IDisposable, IAsyncLifetime
{
    private bool _disposed;

    // Azurite lives in the Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite namespace
    public AzuriteManager AzuriteManager { get; } = new(true);

    public EdiDatabaseManager DatabaseManager { get; set; } = new();

    public IArchivedMessagesClient ArchivedMessagesClient { get; set; } = null!;

    protected AuthenticatedActor AuthenticatedActor { get; set; } = null!;

    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    private TokenValidationParameters DisableAllTokenValidations => new()
    {
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuer = false,
        SignatureValidator = (token, _) => new JsonWebToken(token),
    };

    public void CleanupDatabase()
    {
        var cleanupStatement =
            $"DELETE FROM [dbo].[ArchivedMessages]";

        using var connection = new SqlConnection(DatabaseManager.ConnectionString);
        connection.Open();

        using (var command = new SqlCommand(cleanupStatement, connection))
        {
            command.ExecuteNonQuery();
        }

        connection.Close();
    }

    public void CleanupFileStorage(bool disposing = false)
    {
        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);

        var containers = blobServiceClient.GetBlobContainers();

        foreach (var containerToDelete in containers)
            blobServiceClient.DeleteBlobContainer(containerToDelete.Name);

        if (disposing)
        {
            // Cleanup actual Azurite "database" files
            if (Directory.Exists("__blobstorage__"))
                Directory.Delete("__blobstorage__", true);

            if (Directory.Exists("__queuestorage__"))
                Directory.Delete("__queuestorage__", true);

            if (Directory.Exists("__tablestorage__"))
                Directory.Delete("__tablestorage__", true);

            if (File.Exists("__azurite_db_blob__.json"))
                File.Delete("__azurite_db_blob__.json");

            if (File.Exists("__azurite_db_blob_extent__.json"))
                File.Delete("__azurite_db_blob_extent__.json");

            if (File.Exists("__azurite_db_queue__.json"))
                File.Delete("__azurite_db_queue__.json");

            if (File.Exists("__azurite_db_queue_extent__.json"))
                File.Delete("__azurite_db_queue_extent__.json");

            if (File.Exists("__azurite_db_table__.json"))
                File.Delete("__azurite_db_table__.json");

            if (File.Exists("__azurite_db_table_extent__.json"))
                File.Delete("__azurite_db_table_extent__.json");
        }
        else
        {
            CreateRequiredContainers();
        }
    }

    public async Task InitializeAsync()
    {
        await DatabaseManager.CreateDatabaseAsync();
        AzuriteManager.StartAzurite();
        CleanupFileStorage();
        BuildService();

        ArchivedMessagesClient = ServiceProvider.GetService<IArchivedMessagesClient>()!;
        //AuthenticatedActor = ServiceProvider.GetService<AuthenticatedActor>()!;
        //AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None));
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

    public ServiceProvider BuildService()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FEATUREFLAG_ACTORMESSAGEQUEUE"] = "true", //Needed?
            ["DB_CONNECTION_STRING"] = DatabaseManager.ConnectionString, //Needed?
            ["AZURE_STORAGE_ACCOUNT_CONNECTION_STRING"] = AzuriteManager.BlobStorageConnectionString, //Needed?
            // TODO: fix this
            // Archived messages does not depend on ServiceBus, but the dependency injection in building blocks require it :(
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        services
            .AddScoped<AuthenticatedActor>()
            .AddArchivedMessagesModule(configuration);

        ServiceProvider = services.BuildServiceProvider();

        return ServiceProvider;
    }

    public T GetService<T>()
        where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            CleanupFileStorage(true);
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
