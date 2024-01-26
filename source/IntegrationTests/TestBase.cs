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

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BuildingBlocks.Application.Configuration;
using Dapper;
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.Api.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus.RemoteBusinessServices;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.IncomingMessages.Application.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Configuration;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Configuration;
using Energinet.DataHub.EDI.Process.Application.Configuration;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Google.Protobuf;
using IncomingMessages.Infrastructure.Configuration.DataAccess;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;
using IIntegrationEventHandler = Energinet.DataHub.Core.Messaging.Communication.Subscriber.IIntegrationEventHandler;

namespace Energinet.DataHub.EDI.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly ServiceBusSenderFactoryStub _serviceBusSenderFactoryStub;
        private readonly B2BContext _b2BContext;
        private readonly ProcessContext _processContext;
        private readonly IncomingMessagesContext _incomingMessagesContext;
        private ServiceCollection? _services;
        private bool _disposed;

        protected TestBase(IntegrationTestFixture integrationTestFixture)
        {
            ArgumentNullException.ThrowIfNull(integrationTestFixture);
            integrationTestFixture.CleanupDatabase();
            integrationTestFixture.CleanupFileStorage();
            _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
            TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
            InboxEventNotificationHandler = new TestNotificationHandlerSpy();
            BuildServices(integrationTestFixture.AzuriteManager.BlobStorageConnectionString);
            _b2BContext = GetService<B2BContext>();
            _processContext = GetService<ProcessContext>();
            _incomingMessagesContext = GetService<IncomingMessagesContext>();
            AuthenticatedActor = GetService<AuthenticatedActor>();
            AuthenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), restriction: Restriction.None));
        }

        protected AuthenticatedActor AuthenticatedActor { get; }

        protected ServiceProvider ServiceProvider { get; private set; } = null!;

        private TestAggregatedTimeSeriesRequestAcceptedHandlerSpy TestAggregatedTimeSeriesRequestAcceptedHandlerSpy { get; }

        private TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected static async Task<Response<BlobDownloadInfo>> GetFileFromFileStorageAsync(string container, string fileStorageReference)
        {
            var azuriteBlobConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
            var blobServiceClient = new BlobServiceClient(azuriteBlobConnectionString); // Uses new client to avoid some form of caching or similar

            var containerClient = blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(fileStorageReference);

            var blobContent = await blobClient.DownloadAsync();
            return blobContent;
        }

        protected static async Task<string> GetStreamContentAsStringAsync(Stream stream)
        {
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            var stringContent = await streamReader.ReadToEndAsync();

            return stringContent;
        }

        protected async Task<string> GetArchivedMessageFileStorageReferenceFromDatabaseAsync(Guid messageId)
        {
            using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
            var fileStorageReference = await connection.ExecuteScalarAsync<string>($"SELECT FileStorageReference FROM [dbo].[ArchivedMessages] WHERE MessageId = '{messageId}'");

            return fileStorageReference;
        }

        protected T GetService<T>()
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

            _b2BContext.Dispose();
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
            Environment.SetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_CONNECTION_STRING", fileStorageConnectionString);

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            _services = new ServiceCollection();

            _services.AddTransient<InboxEventsProcessor>();
            _services.AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>>(_ => TestAggregatedTimeSeriesRequestAcceptedHandlerSpy);
            _services.AddTransient<INotificationHandler<TestNotification>>(
                _ => InboxEventNotificationHandler);

            _services.AddTransient<IRequestHandler<TestCommand, Unit>, TestCommandHandler>();
            _services.AddTransient<IRequestHandler<TestCreateOutgoingMessageCommand, Unit>, TestCreateOutgoingCommandHandler>();

            _services.AddScopedSqlDbContext<B2BContext>(config);

            _services.AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();
            _services.AddAuthentication(
                sp => new MarketActorAuthenticator(
                    sp.GetRequiredService<IMasterDataClient>(),
                    sp.GetRequiredService<AuthenticatedActor>(),
                    sp.GetRequiredService<ILogger<MarketActorAuthenticator>>()));

            CompositionRoot.Initialize(_services)
                .AddRemoteBusinessService<DummyRequest, DummyReply>(
                    _ => new RemoteBusinessServiceRequestSenderSpy<DummyRequest>("Dummy"), "Dummy")
                .AddSystemClock(new SystemDateTimeProviderStub())
                .AddCorrelationContext(_ =>
                {
                    var correlation = new CorrelationContext();
                    correlation.SetId(Guid.NewGuid().ToString());
                    return correlation;
                })
                .AddBearerAuthentication(JwtTokenParserTests.DisableAllTokenValidations);

            _services.AddOutgoingMessagesModule(config);
            _services.AddProcessModule(config);
            _services.AddArchivedMessagesModule(config);
            _services.AddIncomingMessagesModule(config);
            _services.AddMasterDataModule(config);

            // Replace the services with stub implementations.
            // - Building blocks
            _services.AddSingleton<IServiceBusSenderFactory>(_serviceBusSenderFactoryStub);

            ServiceProvider = _services.BuildServiceProvider();
        }
    }
}
