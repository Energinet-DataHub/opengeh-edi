﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Models;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces;
using Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Database;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.ArchivedMessages.IntegrationTests.Fixture;

public class ArchivedMessagesFixture : IDisposable, IAsyncLifetime
{
    private readonly Guid _actorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private bool _disposed;

    public AzuriteManager AzuriteManager { get; } = new(true);

    public EdiDatabaseManager DatabaseManager { get; set; } = new("ArchivedMessages.IntegrationTests");

    public IArchivedMessagesClient ArchivedMessagesClient { get; set; } = null!;

    public ServiceProvider Services { get; private set; } = null!;

    private ServiceCollection ServiceCollection { get; set; } = null!;

    public void CleanupDatabase()
    {
        DatabaseManager.CleanupDatabase();
    }

    public void CleanupFileStorage(bool disposing = false)
    {
        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);

        var containers = blobServiceClient.GetBlobContainers();

        foreach (var containerToDelete in containers)
        {
            blobServiceClient.DeleteBlobContainer(containerToDelete.Name);
        }

        if (disposing)
        {
            // Cleanup actual Azurite "database" files
            AzuriteManager.CleanupAzuriteStorage();
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
                    actorNumber: ActorNumber.Create("1234512345888"),
                    restriction: Restriction.None,
                    actorRole: ActorRole.MeteredDataAdministrator,
                    actorClientId: null,
                    actorId: _actorId));

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
        ServiceCollection = new ServiceCollection();

        if (testOutputHelper != null)
            ServiceCollection.AddTestLogger(testOutputHelper);

        var configuration = AddInMemoryConfigurations(ServiceCollection, new Dictionary<string, string?>()
        {
            ["DB_CONNECTION_STRING"] = DatabaseManager.ConnectionString,
            [$"{BlobServiceClientConnectionOptions.SectionName}:{nameof(BlobServiceClientConnectionOptions.StorageAccountUrl)}"] =
                AzuriteManager.BlobStorageServiceUri.AbsoluteUri,
            // TODO: fix this
            // Archived messages does not depend on ServiceBus, but the dependency injection in building blocks require it :(
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
        });

        ServiceCollection
            .AddScoped<IClock>(_ => new ClockStub())
            .AddScoped<AuthenticatedActor>()
            .AddTokenCredentialProvider()
            .AddArchivedMessagesModule(configuration);

        return ServiceCollection.BuildServiceProvider();
    }

    public IConfiguration AddInMemoryConfigurations(IServiceCollection services, Dictionary<string, string?> configurations)
    {
        var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurations)
                .Build();

        services.AddScoped<IConfiguration>(_ => configuration);

        return configuration;
    }

    public async Task<ArchivedMessageDto> CreateArchivedMessageAsync(
        ArchivedMessageTypeDto? archivedMessageType = null,
        string? messageId = null,
        string? documentContent = null,
        DocumentType? documentType = null,
        BusinessReason? businessReason = null,
        string? senderNumber = null,
        ActorRole? senderRole = null,
        string? receiverNumber = null,
        ActorRole? receiverRole = null,
        Instant? timestamp = null,
        MessageId? relatedToMessageId = null,
        bool storeMessage = true,
        IReadOnlyList<MeteringPointId>? meteringPointIds = null)
    {
        var documentStream = new MemoryStream();

        if (!string.IsNullOrEmpty(documentContent))
        {
            var streamWriter = new StreamWriter(documentStream);
            streamWriter.Write(documentContent);
            streamWriter.Flush();
        }

        var archivedMessage = new ArchivedMessageDto(
            string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString() : messageId,
            Array.Empty<EventId>(),
            documentType ?? DocumentType.NotifyAggregatedMeasureData,
            ActorNumber.Create(senderNumber ?? "1234512345123"),
            senderRole ?? ActorRole.MeteredDataAdministrator,
            ActorNumber.Create(receiverNumber ?? "1234512345128"),
            receiverRole ?? ActorRole.DanishEnergyAgency,
            timestamp ?? Instant.FromUtc(2023, 01, 01, 0, 0),
            businessReason ?? BusinessReason.BalanceFixing,
            archivedMessageType ?? ArchivedMessageTypeDto.IncomingMessage,
            new ArchivedMessageStreamDto(documentStream),
            meteringPointIds ?? Array.Empty<MeteringPointId>(),
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

    public async Task<int> GetNumberOfCreatedMessagesInDatabase()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>()!;
        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var rowCount =
            await connection.QuerySingleAsync<int>(
                    "SELECT COUNT(*) FROM dbo.[ArchivedMessages]")
                .ConfigureAwait(false);

        return rowCount;
    }

    public async Task<IReadOnlyCollection<MeteringPointArchivedMessageFromDb>> GetAllMeteringPointMessagesInDatabase()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>()!;
        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var archivedMessages =
            await connection.QueryAsync<MeteringPointArchivedMessageFromDb>(
                    "SELECT * FROM dbo.[MeteringPointArchivedMessages]")
                .ConfigureAwait(false);

        return archivedMessages.ToList().AsReadOnly();
    }

    public async Task<int> GetNumberOfCreatedMeteringPointMessages()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>()!;
        using var connection = await connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);

        var rowCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.[MeteringPointArchivedMessages]")
            .ConfigureAwait(false);

        return rowCount;
    }

    public async Task<ArchivedMessageStreamDto> GetMessagesFromBlob(
        FileStorageReference reference)
    {
        var blobClient = Services.GetService<IFileStorageClient>()!;

        var fileStorageFile = await blobClient.DownloadAsync(reference, CancellationToken.None).ConfigureAwait(false);
        return new ArchivedMessageStreamDto(fileStorageFile);
    }

    public IServiceCollection GetServiceCollectionClone()
    {
        if (ServiceCollection == null)
            throw new InvalidOperationException("ServiceCollection is not yet initialized");

        var serviceCollectionClone = new ServiceCollection { ServiceCollection };

        return serviceCollectionClone;
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
