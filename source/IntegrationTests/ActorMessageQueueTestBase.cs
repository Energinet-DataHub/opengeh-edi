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
using Energinet.DataHub.EDI.ActorMessageQueue.Application.Configuration;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess;
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
using Energinet.DataHub.EDI.Process.Application.Configuration;
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
    [Collection("ActorMessageQueueIntegrationTest")]
    public class ActorMessageQueueTestBase : IDisposable
    {
        private readonly ActorMessageQueueContext _actorMessageQueueContext;
        private ServiceCollection? _services;
        private IServiceProvider _serviceProvider = default!;
        private bool _disposed;

        protected ActorMessageQueueTestBase(ActorMessageQueueDatabaseFixture databaseFixture)
        {
            ArgumentNullException.ThrowIfNull(databaseFixture);
            databaseFixture.CleanupDatabase();
            InboxEventNotificationHandler = new TestNotificationHandlerSpy();
            BuildServices();
            _actorMessageQueueContext = GetService<ActorMessageQueueContext>();
        }

        protected TestNotificationHandlerSpy InboxEventNotificationHandler { get; }

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

            _actorMessageQueueContext.Dispose();
            ((ServiceProvider)_serviceProvider!).Dispose();
            _disposed = true;
        }

        protected async Task InvokeDomainEventAsync(DomainEvent domainEvent)
        {
            BuildServices();
            await GetService<IMediator>().Publish(domainEvent);
            await GetService<Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess.UnitOfWork>().CommitAsync();
        }

        protected Task<TResult> InvokeCommandAsync<TResult>(ICommand<TResult> command)
        {
            BuildServices();
            return GetService<IMediator>().Send(command);
        }

        private void BuildServices()
        {
            Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
            _services = new ServiceCollection();

            ActorMessageQueueConfiguration.Configure(_services, DatabaseFixture.ConnectionString);

            // _services.AddTransient<INotificationHandler<TestNotification>>(_ => InboxEventNotificationHandler);

            // _services.AddTransient<IRequestHandler<TestCommand, Unit>, TestCommandHandler>();
            // _services.AddTransient<IRequestHandler<TestCreateOutgoingMessageCommand, Unit>, TestCreateOutgoingCommandHandler>();

            // _services.AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();

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
        }
    }
}
