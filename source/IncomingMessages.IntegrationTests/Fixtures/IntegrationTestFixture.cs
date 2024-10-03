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

using System.Data.SqlClient;
using Azure.Storage.Blobs;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using HttpClientFactory = Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks.HttpClientFactory;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IDisposable, IAsyncLifetime
{
    private bool _disposed;

    public IntegrationTestFixture()
    {
        IntegrationTestConfiguration = new IntegrationTestConfiguration();

        DatabaseManager = new EdiDatabaseManager();

        DatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            databricksSettings: IntegrationTestConfiguration.DatabricksSettings,
            schemaPrefix: "edi_integration_tests");
    }

    public string DatabaseConnectionString
    {
        get
        {
            var dbConnectionString = DatabaseManager.ConnectionString;
            if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate might be required for some
                dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";
            return dbConnectionString;
        }
    }

    public EdiDatabaseManager DatabaseManager { get; set; }

    public AzuriteManager AzuriteManager { get; } = new(true);

    public IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    public DatabricksSchemaManager DatabricksSchemaManager { get; }

    public void CleanupDatabase()
    {
        var cleanupStatement =
            $"DELETE FROM [dbo].[MessageRegistry] " +
            $"DELETE FROM [dbo].[TransactionRegistry]" +
            $"DELETE FROM [dbo].[OutgoingMessages] " +
            $"DELETE FROM [dbo].[QueuedInternalCommands] " +
            $"DELETE FROM [dbo].[MarketEvaluationPoints]" +
            $"DELETE FROM [dbo].[Actor]" +
            $"DELETE FROM [dbo].[ReceivedIntegrationEvents]" +
            $"DELETE FROM [dbo].[AggregatedMeasureDataProcessGridAreas]" +
            $"DELETE FROM [dbo].[AggregatedMeasureDataProcesses]" +
            $"DELETE FROM [dbo].[ArchivedMessages]" +
            $"DELETE FROM [dbo].[MarketDocuments]" +
            $"DELETE FROM [dbo].[Bundles]" +
            $"DELETE FROM [dbo].[ActorMessageQueues]" +
            $"DELETE FROM [dbo].[ReceivedInboxEvents]" +
            $"DELETE FROM [dbo].[MessageRegistry]" +
            $"DELETE FROM [dbo].[TransactionRegistry]" +
            $"DELETE FROM [dbo].[GridAreaOwner]" +
            $"DELETE FROM [dbo].[ActorCertificate]" +
            $"DELETE FROM [dbo].[WholesaleServicesProcessChargeTypes]" +
            $"DELETE FROM [dbo].[WholesaleServicesProcessGridAreas]" +
            $"DELETE FROM [dbo].[WholesaleServicesProcesses]" +
            $"DELETE FROM [dbo].[Outbox]" +
            $"DELETE FROM [dbo].[ProcessDelegation]";

        using var connection = new SqlConnection(DatabaseManager.ConnectionString);
        connection.Open();

        using (var command = new SqlCommand(cleanupStatement, connection))
        {
            command.ExecuteNonQuery();
        }

        connection.Close();
    }

    public async Task InitializeAsync()
    {
        await DatabaseManager.CreateDatabaseAsync();
        AzuriteManager.StartAzurite();
        CleanupFileStorage();
    }

    public async Task DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
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
            FileStorageCategory.OutgoingMessage(),
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
