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
using Azure.Storage.Blobs;
using Dapper;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using Xunit.Abstractions;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;
using SampleData = Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.SampleData;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests;

[Collection(nameof(OutgoingMessagesIntegrationTestCollection))]
public class OutgoingMessagesTestBase : IDisposable
{
    private ServiceCollection? _services;
    private bool _disposed;

    protected OutgoingMessagesTestBase(OutgoingMessagesTestFixture outgoingMessagesTestFixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = outgoingMessagesTestFixture;

        Fixture.DatabaseManager.CleanupDatabase();
        Fixture.CleanupFileStorage();
        BuildServices(testOutputHelper);
        AuthenticatedActor = GetService<AuthenticatedActor>();
        AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None, ActorRole.EnergySupplier, Guid.Parse("00000000-0000-0000-0000-000000000001")));
    }

    protected OutgoingMessagesTestFixture Fixture { get; }

    protected FeatureFlagManagerStub FeatureFlagManagerStub { get; } = new();

    protected AuthenticatedActor AuthenticatedActor { get; }

    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected static async Task<string> GetStreamContentAsStringAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek && stream.Position != 0)
            stream.Position = 0;

        using var streamReader = new StreamReader(stream, Encoding.UTF8);
        var stringContent = await streamReader.ReadToEndAsync();

        return stringContent;
    }

    protected async Task<string> GetFileContentFromFileStorageAsync(
        string container,
        string fileStorageReference)
    {
        var clientFactory = ServiceProvider.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();
        var options = ServiceProvider.GetRequiredService<IOptions<BlobServiceClientConnectionOptions>>();
        var blobServiceClient = clientFactory.CreateClient(options.Value.ClientName);
        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(fileStorageReference);

        var blobContent = await blobClient.DownloadAsync();

        if (!blobContent.HasValue)
        {
            throw new InvalidOperationException(
                $"Couldn't get file content from file storage (container: {container}, blob: {fileStorageReference})");
        }

        return await GetStreamContentAsStringAsync(blobContent.Value.Content);
    }

    protected async Task<string?> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return fileStorageReference;
    }

    protected async Task<string?> GetMarketDocumentFileStorageReferenceFromDatabaseAsync(MessageId messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT md.FileStorageReference "
            + $"FROM [dbo].[MarketDocuments] md JOIN [dbo].[Bundles] b ON md.BundleId = b.Id "
            + $"WHERE b.MessageId = '{messageId.Value}'");

        return fileStorageReference;
    }

    protected async Task<Guid> GetIdOfArchivedMessageFromDatabaseAsync(string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var id = await connection.ExecuteScalarAsync<Guid>($"SELECT Id FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return id;
    }

    protected async Task<dynamic?> GetArchivedMessageFromDatabaseAsync(string messageId)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var archivedMessage = await connection.QuerySingleOrDefaultAsync($"SELECT * FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

        return archivedMessage;
    }

    protected Task<PeekResultDto?> PeekMessageAsync(MessageCategory category, ActorNumber? actorNumber = null, ActorRole? actorRole = null, DocumentFormat? documentFormat = null)
    {
        ClearDbContextCaches();

        var outgoingMessagesClient = GetService<IOutgoingMessagesClient>();
        var authenticatedActor = GetService<AuthenticatedActor>();
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(actorNumber ?? ActorNumber.Create(SampleData.NewEnergySupplierNumber), restriction: Restriction.Owned, actorRole ?? ActorRole.EnergySupplier, Guid.Parse("00000000-0000-0000-0000-000000000001")));
        return outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(actorNumber ?? ActorNumber.Create(SampleData.NewEnergySupplierNumber), category, actorRole ?? ActorRole.EnergySupplier, documentFormat ?? DocumentFormat.Xml), CancellationToken.None);
    }

    protected T GetService<T>()
        where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected void ClearDbContextCaches()
    {
        if (_services == null)
            throw new InvalidOperationException("ServiceCollection is not yet initialized");

        var dbContextServices = _services
            .Where(s => s.ServiceType.IsSubclassOf(typeof(DbContext)) || s.ServiceType == typeof(DbContext))
            .Select(s => (DbContext)ServiceProvider.GetService(s.ServiceType)!);

        foreach (var dbContext in dbContextServices)
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        ServiceProvider.Dispose();
        _disposed = true;
    }

    private void BuildServices(ITestOutputHelper testOutputHelper)
    {
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", Fixture.DatabaseManager.ConnectionString);
        Environment.SetEnvironmentVariable(
            $"{BlobServiceClientConnectionOptions.SectionName}__{nameof(BlobServiceClientConnectionOptions.StorageAccountUrl)}",
            Fixture.AzuriteManager.BlobStorageServiceUri.AbsoluteUri);

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    // ServiceBus
                    [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
                })
            .Build();

        _services = [];
        _services
            .AddScoped<IConfiguration>(_ => config)
            .AddB2BAuthentication(
                new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuer = false,
                    SignatureValidator = (token, _) => new JsonWebToken(token),
                })
            .AddJavaScriptEncoder()
            .AddSerializer()
            .AddTestLogger(testOutputHelper)
            .AddScoped<IClock>(_ => new ClockStub())
            .AddOutgoingMessagesModule(config)
            .AddArchivedMessagesModule(config)
            .AddMasterDataModule(config);

        // Replace the services with stub implementations.
        _services.AddTransient<IFeatureFlagManager>(_ => FeatureFlagManagerStub);

        _services.AddScoped<ExecutionContext>(_ =>
        {
            var executionContext = new ExecutionContext();
            executionContext.SetExecutionType(ExecutionType.Test);
            return executionContext;
        });

        _services.AddSingleton<TelemetryClient>(x =>
        {
            return new TelemetryClient(
                new TelemetryConfiguration { TelemetryChannel = new TelemetryChannelStub(), });
        });

        ServiceProvider = _services.BuildServiceProvider();
    }
}
