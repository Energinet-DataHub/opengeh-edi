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

using System.Text;
using Azure.Storage.Blobs;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using BuildingBlocks.Application.FeatureFlag;
using Dapper;
using Energinet.DataHub.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None, ActorRole.EnergySupplier));
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

    protected static async Task<string> GetFileContentFromFileStorageAsync(
        string container,
        string fileStorageReference)
    {
        var azuriteBlobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
        var blobServiceClient = new BlobServiceClient(azuriteBlobConnectionString); // Uses new client to avoid some form of caching or similar

        var containerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(fileStorageReference);

        var blobContent = await blobClient.DownloadAsync();

        if (!blobContent.HasValue)
            throw new InvalidOperationException($"Couldn't get file content from file storage (container: {container}, blob: {fileStorageReference})");

        var fileStringContent = await GetStreamContentAsStringAsync(blobContent.Value.Content);
        return fileStringContent;
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
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(actorNumber ?? ActorNumber.Create(SampleData.NewEnergySupplierNumber), restriction: Restriction.Owned, actorRole ?? ActorRole.EnergySupplier));
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
            dbContext.ChangeTracker.Clear();
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
        Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", Fixture.AzuriteManager.BlobStorageConnectionString);

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
            .AddTransient<InboxEventsProcessor>()
            .AddScopedSqlDbContext<ProcessContext>(config)
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
            .AddTransient<INotificationHandler<ADayHasPassed>, ExecuteDataRetentionsWhenADayHasPassed>()
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

        ServiceProvider = _services.BuildServiceProvider();
    }
}
