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

using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using BuildingBlocks.Application.FeatureFlag;
using Dapper;
using Energinet.DataHub.BuildingBlocks.Tests;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Domain.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using Xunit.Abstractions;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;

namespace Energinet.DataHub.EDI.IncomingMessages.IntegrationTests;

[Collection("IntegrationTest")]
public class TestBase : IDisposable
{
    private readonly IAzureClientFactory<ServiceBusSender> _serviceBusSenderFactoryStub;
    private readonly IncomingMessagesContext _incomingMessagesContext;
    private ServiceCollection? _services;
    private bool _disposed;

    protected TestBase(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = integrationTestFixture;

        Fixture.DatabaseManager.CleanupDatabase();
        Fixture.CleanupFileStorage();
        _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
        // TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
        // InboxEventNotificationHandler = new TestNotificationHandlerSpy();
        BuildServices(testOutputHelper);
        _incomingMessagesContext = GetService<IncomingMessagesContext>();
        AuthenticatedActor = GetService<AuthenticatedActor>();
        AuthenticatedActor.SetAuthenticatedActor(
            new ActorIdentity(ActorNumber.Create("1234512345888"), Restriction.None, ActorRole.EnergySupplier));
    }

    protected IntegrationTestFixture Fixture { get; }

    protected FeatureFlagManagerStub FeatureFlagManagerStub { get; } = new();

    protected AuthenticatedActor AuthenticatedActor { get; }

    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    // private TestAggregatedTimeSeriesRequestAcceptedHandlerSpy TestAggregatedTimeSeriesRequestAcceptedHandlerSpy { get; }

    // private TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected static async Task<string> GetFileContentFromFileStorageAsync(
        string container,
        string fileStorageReference)
    {
        var azuriteBlobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
        var blobServiceClient =
            new BlobServiceClient(
                azuriteBlobConnectionString); // Uses new client to avoid some form of caching or similar

        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(fileStorageReference);

        var blobContent = await blobClient.DownloadAsync();

        if (!blobContent.HasValue)
        {
            throw new InvalidOperationException(
                $"Couldn't get file content from file storage (container: {container}, blob: {fileStorageReference})");
        }

        var fileStringContent = await GetStreamContentAsStringAsync(blobContent.Value.Content);
        return fileStringContent;
    }

    protected static async Task<string> GetStreamContentAsStringAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek && stream.Position != 0)
        {
            stream.Position = 0;
        }

        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var stringContent = await streamReader.ReadToEndAsync();

        return stringContent;
    }

    protected async Task<string?> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(string messageId)
    {
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var fileStorageReference = await connection.ExecuteScalarAsync<string>(
            $"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return fileStorageReference;
    }

    protected async Task<dynamic?> GetArchivedMessageFromDatabaseAsync(string messageId)
    {
        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var archivedMessage = await connection.QuerySingleOrDefaultAsync(
            $"SELECT * FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return archivedMessage;
    }

    protected T GetService<T>()
        where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected void ClearDbContextCaches()
    {
        if (_services == null)
        {
            throw new InvalidOperationException("ServiceCollection is not yet initialized");
        }

        var dbContextServices = _services
            .Where(s => s.ServiceType.IsSubclassOf(typeof(DbContext)) || s.ServiceType == typeof(DbContext))
            .Select(s => (DbContext)ServiceProvider.GetService(s.ServiceType)!);

        foreach (var dbContext in dbContextServices)
            dbContext.ChangeTracker.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _incomingMessagesContext.Dispose();
        ServiceProvider.Dispose();
        _disposed = true;
    }

    private void BuildServices(ITestOutputHelper testOutputHelper)
    {
        Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", Fixture.DatabaseManager.ConnectionString);
        Environment.SetEnvironmentVariable(
            "AZURE_STORAGE_ACCOUNT_CONNECTION_STRING",
            Fixture.AzuriteManager.BlobStorageConnectionString);

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    // ServiceBus
                    [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] =
                        "Fake",
                    [$"{EdiInboxQueueOptions.SectionName}:{nameof(EdiInboxQueueOptions.QueueName)}"] = "Fake",
                    [$"{WholesaleInboxQueueOptions.SectionName}:{nameof(WholesaleInboxQueueOptions.QueueName)}"] =
                        "Fake",
                    [$"{IncomingMessagesQueueOptions.SectionName}:{nameof(IncomingMessagesQueueOptions.QueueName)}"] =
                        "Fake",
                    [$"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}"] =
                        "NotEmpty",
                    [$"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}"] =
                        "NotEmpty",

                    // Dead-letter logging
                    [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}"] =
                        Fixture.AzuriteManager.BlobStorageServiceUri.OriginalString,
                    [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.ContainerName)}"] =
                        "edi-tests",
                })
            .Build();

        _services = [];
        _services
            .AddScoped<IConfiguration>(_ => config)
            .AddTransient<InboxEventsProcessor>()
            .AddB2BAuthentication(new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuer = false,
                SignatureValidator = (token, _) => new JsonWebToken(token),
            })
             .AddJavaScriptEncoder()
             .AddSerializer()
            .AddScoped<IClock>(_ => new ClockStub())
            .AddArchivedMessagesModule(config)
            .AddMasterDataModule(config)
            .AddIncomingMessagesModule(config);

        // Replace the services with stub implementations.
        // - Building blocks
        _services.AddSingleton(_serviceBusSenderFactoryStub);
        _services.AddTransient<IFeatureFlagManager>(_ => FeatureFlagManagerStub);

        _services.AddScoped<ExecutionContext>(
            _ =>
            {
                var executionContext = new ExecutionContext();
                executionContext.SetExecutionType(ExecutionType.Test);
                return executionContext;
            });

        // Add test logger
        _services.AddSingleton<ITestOutputHelper>(_ => testOutputHelper);
        _services.Add(ServiceDescriptor.Singleton(typeof(Logger<>), typeof(Logger<>)));
        _services.Add(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(TestLogger<>)));

        ServiceProvider = _services.BuildServiceProvider();
    }
}
