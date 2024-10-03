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
using Dapper;
using Energinet.DataHub.BuildingBlocks.Tests;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture.Database;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;

public class ArchivedMessagesFixture : IDisposable, IAsyncLifetime
{
    private bool _disposed;

    public AzuriteManager AzuriteManager { get; } = new(true);

    public EdiDatabaseManager DatabaseManager { get; set; } = new();

    public IArchivedMessagesClient ArchivedMessagesClient { get; set; } = null!;

    public ServiceProvider Services { get; private set; } = null!;

    public void CleanupDatabase()
    {
        DatabaseManager.CleanupDatabase();
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
        Services = BuildService();

        Services.GetRequiredService<AuthenticatedActor>()
            .SetAuthenticatedActor(
                new ActorIdentity(
                    ActorNumber.Create("1234512345888"),
                    restriction: Restriction.None,
                    ActorRole.MeteredDataAdministrator));

        ArchivedMessagesClient = Services.GetRequiredService<IArchivedMessagesClient>();
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

    public ServiceProvider BuildService(ITestOutputHelper? testOutputHelper = null)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DB_CONNECTION_STRING"] = DatabaseManager.ConnectionString,
            ["AZURE_STORAGE_ACCOUNT_CONNECTION_STRING"] = AzuriteManager.BlobStorageConnectionString,
            // TODO: fix this
            // Archived messages does not depend on ServiceBus, but the dependency injection in building blocks require it :(
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        if (testOutputHelper != null)
        {
            services.AddSingleton(sp => testOutputHelper);
            services.Add(ServiceDescriptor.Singleton(typeof(Logger<>), typeof(Logger<>)));
            services.Add(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(TestLogger<>)));
        }

        services
            .AddScoped<AuthenticatedActor>()
            .AddArchivedMessagesModule(configuration);

        return services.BuildServiceProvider();
    }

    public async Task<ArchivedMessage> CreateArchivedMessageAsync(
        ArchivedMessageType? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        string? documentType = null,
        string? businessReasons = null,
        string? senderNumber = null,
        ActorRole? senderRole = null,
        string? receiverNumber = null,
        ActorRole? receiverRole = null,
        Instant? timestamp = null,
        MessageId? relatedToMessageId = null,
        bool storeMessage = true)
    {
        var documentStream = new MemoryStream();

        if (!string.IsNullOrEmpty(documentContent))
        {
            var streamWriter = new StreamWriter(documentStream);
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        var archivedMessage = new ArchivedMessage(
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            Array.Empty<EventId>(),
            documentType ?? DocumentType.NotifyAggregatedMeasureData.Name,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            senderRole ?? ActorRole.MeteredDataAdministrator,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            receiverRole ?? ActorRole.DanishEnergyAgency,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            businessReasons ?? BusinessReason.BalanceFixing.Name,
            archivedMessageType ?? ArchivedMessageType.IncomingMessage,
            new ArchivedMessageStream(documentStream),
            relatedToMessageId ?? null);

        if (storeMessage)
            await ArchivedMessagesClient.CreateAsync(archivedMessage, CancellationToken.None);

        return archivedMessage;
    }

    public async Task<IReadOnlyCollection<ArchivedMessageFromDb>> GetAllMessagesInDatabase()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>()!;
        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var archivedMessages =
            await connection.QueryAsync<ArchivedMessageFromDb>(
                    "SELECT * FROM dbo.[ArchivedMessages]")
                .ConfigureAwait(false);

        return archivedMessages.ToList().AsReadOnly();
    }

    public async Task<ArchivedMessageStream> GetMessagesFromBlob(FileStorageReference reference)
    {
        var blobClient = Services.GetService<IFileStorageClient>()!;

        var fileStorageFile = await blobClient.DownloadAsync(reference).ConfigureAwait(false);
        return new ArchivedMessageStream(fileStorageFile);
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
