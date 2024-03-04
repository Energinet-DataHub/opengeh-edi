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

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using BuildingBlocks.Application.FeatureFlag;
using Dapper;
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.Api.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Configuration;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Google.Protobuf;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly ServiceBusSenderFactoryStub _serviceBusSenderFactoryStub;
        private readonly ProcessContext _processContext;
        private readonly IncomingMessagesContext _incomingMessagesContext;
        private ServiceCollection? _services;
        private bool _disposed;

        protected TestBase(IntegrationTestFixture integrationTestFixture)
        {
            ArgumentNullException.ThrowIfNull(integrationTestFixture);
            IntegrationTestFixture.CleanupDatabase();
            integrationTestFixture.CleanupFileStorage();
            _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
            TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
            InboxEventNotificationHandler = new TestNotificationHandlerSpy();
            BuildServices(integrationTestFixture.AzuriteManager.BlobStorageConnectionString);
            _processContext = GetService<ProcessContext>();
            _incomingMessagesContext = GetService<IncomingMessagesContext>();
            AuthenticatedActor = GetService<AuthenticatedActor>();
            AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None));
        }

        protected FeatureFlagManagerStub FeatureFlagManagerStub { get; } = new();

        protected AuthenticatedActor AuthenticatedActor { get; }

        protected ServiceProvider ServiceProvider { get; private set; } = null!;

        private TestAggregatedTimeSeriesRequestAcceptedHandlerSpy TestAggregatedTimeSeriesRequestAcceptedHandlerSpy { get; }

        private TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static async Task<string> GetFileContentFromFileStorageAsync(string container, string fileStorageReference)
        {
            var azuriteBlobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
            var blobServiceClient = new BlobServiceClient(azuriteBlobConnectionString); // Uses new client to avoid some form of caching or similar

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(fileStorageReference);

            var blobContent = await blobClient.DownloadAsync();

            if (!blobContent.HasValue) throw new InvalidOperationException($"Couldn't get file content from file storage (container: {container}, blob: {fileStorageReference})");

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

        protected async Task<string> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(string messageId)
        {
            using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
            var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

            return fileStorageReference;
        }

        protected async Task<string> GetMarketDocumentFileStorageReferenceFromDatabaseAsync(Guid bundleId)
        {
            using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
            var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[MarketDocuments] WHERE BundleId = '{bundleId}'");

            return fileStorageReference;
        }

        protected async Task<Guid> GetArchivedMessageIdFromDatabaseAsync(string messageId)
        {
            using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
            var id = await connection.ExecuteScalarAsync<Guid>($"SELECT Id FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

            return id;
        }

        protected Task<string> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(Guid messageId) => GetArchivedMessageFileStorageReferenceFromDatabaseAsync(messageId.ToString());

        protected T GetService<T>()
            where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        protected void ClearDbContextCaches()
        {
            if (_services == null) throw new InvalidOperationException("ServiceCollection is not yet initialized");

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

            _processContext.Dispose();
            _incomingMessagesContext.Dispose();
            _serviceBusSenderFactoryStub.Dispose();
            ServiceProvider.Dispose();
            _disposed = true;
        }

        protected IServiceCollection GetServiceCollectionClone()
        {
            if (_services == null) throw new InvalidOperationException("ServiceCollection is not yet initialized");

            var serviceCollectionClone = new ServiceCollection { _services };

            return serviceCollectionClone;
        }

        protected Task<TResult> InvokeCommandAsync<TResult>(ICommand<TResult> command)
        {
            return GetService<IMediator>().Send(command);
        }

        protected Task CreateActorIfNotExistAsync(CreateActorDto createActorDto)
        {
            return GetService<IMasterDataClient>().CreateActorIfNotExistAsync(createActorDto, CancellationToken.None);
        }

        protected async Task HavingReceivedInboxEventAsync(string eventType, IMessage eventPayload, Guid processId)
        {
            await GetService<InboxEventReceiver>().
                ReceiveAsync(
                    Guid.NewGuid().ToString(),
                    eventType,
                    processId,
                    eventPayload.ToByteArray())
                .ConfigureAwait(false);
            await ProcessReceivedInboxEventsAsync().ConfigureAwait(false);
            await ProcessInternalCommandsAsync().ConfigureAwait(false);
        }

        private Task ProcessReceivedInboxEventsAsync()
        {
            return ProcessBackgroundTasksAsync();
        }

        private async Task ProcessInternalCommandsAsync()
        {
            await ProcessBackgroundTasksAsync();

            if (_processContext.QueuedInternalCommands.Any(command => command.ProcessedDate == null))
            {
                await ProcessInternalCommandsAsync();
            }
        }

        private Task ProcessBackgroundTasksAsync()
        {
            var datetimeProvider = GetService<ISystemDateTimeProvider>();
            return GetService<IMediator>().Publish(new TenSecondsHasHasPassed(datetimeProvider.Now()));
        }

        private void BuildServices(string fileStorageConnectionString)
        {
            Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
            Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", IntegrationTestFixture.DatabaseConnectionString);
            Environment.SetEnvironmentVariable("WHOLESALE_INBOX_MESSAGE_QUEUE_NAME", "Fake");
            Environment.SetEnvironmentVariable("INCOMING_MESSAGES_QUEUE_NAME", "Fake");
            Environment.SetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE", "Fake");
            Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", fileStorageConnectionString);

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            _services = new ServiceCollection();

            _services.AddTransient<InboxEventsProcessor>()
                .AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>>(_ => TestAggregatedTimeSeriesRequestAcceptedHandlerSpy)
                .AddTransient<INotificationHandler<TestNotification>>(_ => InboxEventNotificationHandler)
                .AddTransient<IRequestHandler<TestCommand, Unit>, TestCommandHandler>()
                .AddTransient<IRequestHandler<TestCreateOutgoingMessageCommand, Unit>, TestCreateOutgoingCommandHandler>()
                .AddScopedSqlDbContext<ProcessContext>(config)
                .AddB2BAuthentication(JwtTokenParserTests.DisableAllTokenValidations);

            CompositionRoot.Initialize(_services)
                .AddSystemClock(new SystemDateTimeProviderStub());

            _services.AddScoped(_ => new JwtTokenParser(JwtTokenParserTests.DisableAllTokenValidations));
            _services.AddScoped(_ =>
            {
                var correlation = new CorrelationContext();
                correlation.SetId(Guid.NewGuid().ToString());
                return correlation;
            })
            .AddIntegrationEventModule()
            .AddOutgoingMessagesModule(config)
            .AddProcessModule(config)
            .AddArchivedMessagesModule(config)
            .AddIncomingMessagesModule(config)
            .AddMasterDataModule(config)
            .AddDataAccessModule(config);

            // Replace the services with stub implementations.
            // - Building blocks
            _services.AddSingleton<IServiceBusSenderFactory>(_serviceBusSenderFactoryStub);
            _services.AddTransient<IFeatureFlagManager>((x) => FeatureFlagManagerStub);

            ServiceProvider = _services.BuildServiceProvider();
        }
    }
}
