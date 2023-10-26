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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.Queries;
using Energinet.DataHub.EDI.Application.Configuration.TimeEvents;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.MessageBus;
using Energinet.DataHub.EDI.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.Aggregations;
using Google.Protobuf;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests
{
    [Collection("ProcessIntegrationTest")]
    public class ProcessTestBase : IDisposable
    {
        private readonly ServiceBusSenderFactoryStub _serviceBusSenderFactoryStub;
        private readonly ProcessContext _processContext;
        private ServiceCollection? _services;
        private IServiceProvider _serviceProvider = default!;
        private bool _disposed;

        protected ProcessTestBase(ProcessDatabaseFixture databaseFixture)
        {
            ArgumentNullException.ThrowIfNull(databaseFixture);
            databaseFixture.CleanupDatabase();
            _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
            TestAggregatedTimeSeriesRequestAcceptedHandlerSpy = new TestAggregatedTimeSeriesRequestAcceptedHandlerSpy();
            InboxEventNotificationHandler = new IntegrationTests.Infrastructure.InboxEvents.TestNotificationHandlerSpy();
            BuildServices();
            _processContext = GetService<ProcessContext>();
        }

        protected TestAggregatedTimeSeriesRequestAcceptedHandlerSpy TestAggregatedTimeSeriesRequestAcceptedHandlerSpy { get; }

        protected IntegrationTests.Infrastructure.InboxEvents.TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public T GetService<T>()
            where T : notnull
        {
            return _serviceProvider!.GetRequiredService<T>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            _processContext.Dispose();
            _serviceBusSenderFactoryStub.Dispose();
            ((ServiceProvider)_serviceProvider!).Dispose();
            _disposed = true;
        }

        protected Task<TResult> InvokeCommandAsync<TResult>(ICommand<TResult> command)
        {
            BuildServices();
            return GetService<IMediator>().Send(command);
        }

        protected Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
        {
            return GetService<IMediator>().Send(query, CancellationToken.None);
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

        protected Task ProcessReceivedInboxEventsAsync()
        {
            return ProcessBackgroundTasksAsync();
        }

        private static string CreateFakeServiceBusConnectionString()
        {
            return new StringBuilder()
                .Append(CultureInfo.InvariantCulture, $"Endpoint=sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/;")
                .Append("SharedAccessKeyName=send;")
                .Append(CultureInfo.InvariantCulture, $"SharedAccessKey={Guid.NewGuid():N}")
                .ToString();
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

        private void BuildServices()
        {
            Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
            _services = new ServiceCollection();

            _services.AddSingleton(new WholesaleServiceBusClientConfiguration("Fake"));
            _services.AddSingleton<IServiceBusSenderFactory>(_serviceBusSenderFactoryStub);
            _services.AddSingleton(
                _ => new ServiceBusClient(CreateFakeServiceBusConnectionString()));

            _services.AddTransient<InboxEventsProcessor>();
            _services.AddTransient<AggregatedTimeSeriesRequestAcceptedEventMapper>();
            _services.AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>>(_ => TestAggregatedTimeSeriesRequestAcceptedHandlerSpy);
            _services.AddTransient<INotificationHandler<IntegrationTests.Infrastructure.InboxEvents.TestNotification>>(
                _ => InboxEventNotificationHandler);

            _services.AddTransient<IRequestHandler<TestCommand, Unit>, TestCommandHandler>();
            _services.AddTransient<IRequestHandler<TestCreateOutgoingMessageCommand, Unit>, TestCreateOutgoingCommandHandler>();

            _services.AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();
            _services.AddTransient<ProcessContext>();
            _services.AddTransient<B2BContext>();

            CompositionRoot.Initialize(_services)
                .AddAuthentication()
                .AddRemoteBusinessService<DummyRequest, DummyReply>(
                    sp => new RemoteBusinessServiceRequestSenderSpy<DummyRequest>("Dummy"), "Dummy")
                .AddDatabaseConnectionFactory(DatabaseFixture.ConnectionString)
                .AddDatabaseContext(DatabaseFixture.ConnectionString)
                .AddSystemClock(new SystemDateTimeProviderStub())
                .AddCorrelationContext(_ =>
                {
                    var correlation = new CorrelationContext();
                    correlation.SetId(Guid.NewGuid().ToString());
                    return correlation;
                })
                .AddMessagePublishing()
                .AddMessageParserServices();
            _serviceProvider = _services.BuildServiceProvider();
            ProcessConfiguration.Configure(_services, DatabaseFixture.ConnectionString);
        }
    }
}
