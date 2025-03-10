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
using Azure.Messaging.ServiceBus;
using Dapper;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.Core.Outbox.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.DataRetention;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationTests.DataRetention;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;

namespace Energinet.DataHub.EDI.IntegrationTests;

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
        BuildServices(testOutputHelper);
        _incomingMessagesContext = GetService<IncomingMessagesContext>();
        AuthenticatedActor = GetService<AuthenticatedActor>();
        AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None, ActorRole.EnergySupplier, null, ActorId));
    }

    protected Guid ActorId => Guid.Parse("00000000-0000-0000-0000-000000000001");

    protected IntegrationTestFixture Fixture { get; }

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

    protected async Task<Guid> GetArchivedMessageIdFromDatabaseAsync(string messageId)
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
        authenticatedActor.SetAuthenticatedActor(new ActorIdentity(actorNumber ?? ActorNumber.Create(SampleData.NewEnergySupplierNumber), restriction: Restriction.Owned, actorRole ?? ActorRole.EnergySupplier, null, ActorId));
        return outgoingMessagesClient.PeekAndCommitAsync(new PeekRequestDto(actorNumber ?? ActorNumber.Create(SampleData.NewEnergySupplierNumber), category, actorRole ?? ActorRole.EnergySupplier, documentFormat ?? DocumentFormat.Xml), CancellationToken.None);
    }

    protected Task<string?> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(Guid messageId)
    {
        return GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId.ToString());
    }

    protected T GetService<T>()
        where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected IEnumerable<T> GetServices<T>()
        where T : notnull
    {
        return ServiceProvider.GetServices<T>();
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

        _incomingMessagesContext.Dispose();
        ServiceProvider.Dispose();
        _disposed = true;
    }

    protected IServiceCollection GetServiceCollectionClone()
    {
        if (_services == null)
            throw new InvalidOperationException("ServiceCollection is not yet initialized");

        var serviceCollectionClone = new ServiceCollection { _services };

        return serviceCollectionClone;
    }

    protected Task CreateActorIfNotExistAsync(CreateActorDto createActorDto)
    {
        return GetService<IMasterDataClient>().CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
    }

    private void BuildServices(ITestOutputHelper testOutputHelper)
    {
        Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
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
                    [$"{IncomingMessagesQueueOptions.SectionName}:{nameof(IncomingMessagesQueueOptions.QueueName)}"] = "Fake",
                    [$"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}"] = "NotEmpty",
                    [$"{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}"] = "NotEmpty",

                    // Dead-letter logging
                    [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}"] = Fixture.AzuriteManager.BlobStorageServiceUri.OriginalString,
                    [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.ContainerName)}"] = "edi-tests",

                    // Databricks
                    [nameof(DatabricksSqlStatementOptions.WorkspaceUrl)] = Fixture.IntegrationTestConfiguration.DatabricksSettings.WorkspaceUrl,
                    [nameof(DatabricksSqlStatementOptions.WorkspaceToken)] = Fixture.IntegrationTestConfiguration.DatabricksSettings.WorkspaceAccessToken,
                    [nameof(DatabricksSqlStatementOptions.WarehouseId)] = Fixture.IntegrationTestConfiguration.DatabricksSettings.WarehouseId,
                    // => EDI views
                    [$"{EdiDatabricksOptions.SectionName}:{nameof(EdiDatabricksOptions.DatabaseName)}"] = Fixture.DatabricksSchemaManager.SchemaName,
                    [$"{EdiDatabricksOptions.SectionName}:{nameof(EdiDatabricksOptions.CatalogName)}"] = "hive_metastore",
                    // => Calculation Result views
                    [$"{nameof(DeltaTableOptions.DatabricksCatalogName)}"] = "hive_metastore",
                })
            .Build();

        _services = [];
        _services.AddScoped<IConfiguration>(_ => config);

        _services
            .AddB2BAuthentication(JwtTokenParserTests.DisableAllTokenValidations)
            .AddJavaScriptEncoder()
            .AddSerializer()
            .AddLogging()
            .AddScoped<IClock>(_ => new ClockStub());

        _services.AddTransient<ExecuteDataRetentionJobs>()
            .AddIntegrationEventModule(config)
            .AddOutgoingMessagesModule(config)
            .AddArchivedMessagesModule(config)
            .AddIncomingMessagesModule(config)
            .AddMasterDataModule(config)
            .AddDataAccessUnitOfWorkModule()
            .AddAuditLog()
            .AddOutboxContext(config)
            .AddOutboxClient<OutboxContext>()
            .AddOutboxProcessor<OutboxContext>();

        // Replace the services with stub implementations.
        // - Building blocks
        _services.AddSingleton<IAzureClientFactory<ServiceBusSender>>(_serviceBusSenderFactoryStub);
        _services.AddTransient<IFeatureFlagManager>((x) => FeatureFlagManagerStub);

        _services.AddScoped<ExecutionContext>((x) =>
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

        // Add test logger
        _services.AddTestLogger(testOutputHelper);

        _services.AddTransient<IDataRetention, SleepyDataRetentionJob>();

        ServiceProvider = _services.BuildServiceProvider();
    }
}
