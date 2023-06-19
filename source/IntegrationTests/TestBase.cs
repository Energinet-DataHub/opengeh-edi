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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Api.Configuration.Middleware.Correlation;
using Application.Configuration;
using Application.Configuration.Commands.Commands;
using Application.Configuration.DataAccess;
using Application.Configuration.Queries;
using Application.Configuration.TimeEvents;
using Application.Transactions.MoveIn;
using Azure.Messaging.ServiceBus;
using Google.Protobuf;
using Infrastructure.Configuration;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.Configuration.IntegrationEvents;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Infrastructure.Transactions.MoveIn;
using Infrastructure.WholeSale;
using IntegrationTests.Fixtures;
using IntegrationTests.Infrastructure.Configuration.IntegrationEvents;
using IntegrationTests.Infrastructure.Configuration.InternalCommands;
using IntegrationTests.TestDoubles;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly ServiceBusSenderFactoryStub _serviceBusSenderFactoryStub;
        private readonly HttpClientSpy _httpClientSpy;
        private ServiceCollection? _services;
        private IServiceProvider _serviceProvider = default!;
        private bool _disposed;

        protected TestBase(DatabaseFixture databaseFixture)
        {
            ArgumentNullException.ThrowIfNull(databaseFixture);
            databaseFixture.CleanupDatabase();
            _httpClientSpy = new HttpClientSpy();
            _serviceBusSenderFactoryStub = new ServiceBusSenderFactoryStub();
            NotificationHandlerSpy = new TestNotificationHandlerSpy();
            BuildServices();
        }

        protected TestNotificationHandlerSpy NotificationHandlerSpy { get; }

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

        protected async Task HavingReceivedIntegrationEventAsync(string eventType, IMessage eventPayload)
        {
            await GetService<IntegrationEventReceiver>().ReceiveAsync(Guid.NewGuid().ToString(), eventType, eventPayload.ToByteArray()).ConfigureAwait(false);
            await ProcessReceivedIntegrationEventsAsync().ConfigureAwait(false);
            await HavingProcessedInternalTasksAsync().ConfigureAwait(false);
        }

        protected Task HavingProcessedInternalTasksAsync()
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

        private Task ProcessReceivedIntegrationEventsAsync()
        {
            return ProcessBackgroundTasksAsync();
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

            _services.AddSingleton(new EnergySupplyingServiceBusClientConfiguration("Fake"));
            _services.AddSingleton(new WholeSaleServiceBusClientConfiguration("Fake"));
            _services.AddSingleton<IServiceBusSenderFactory>(_serviceBusSenderFactoryStub);
            _services.AddSingleton(
                _ => new ServiceBusClient(CreateFakeServiceBusConnectionString()));

            _services.AddTransient<INotificationHandler<TestNotification>>(_ => NotificationHandlerSpy);

            CompositionRoot.Initialize(_services)
                .AddAuthentication()
                .AddAggregationsConfiguration()
                .AddPeekConfiguration(
                    new BundleConfigurationStub(),
                    sp => new BundledMessagesStub(
                        sp.GetRequiredService<IDatabaseConnectionFactory>(),
                        sp.GetRequiredService<B2BContext>()))
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
                .AddRequestHandler<TestCommandHandler>()
                .AddHttpClientAdapter(_ => _httpClientSpy)
                .AddAggregatedMeasureDataServices()
                .AddMoveInServices(
                    new MoveInSettings(
                        new MessageDelivery(new GridOperator() { GracePeriodInDaysAfterEffectiveDateIfNotUpdated = 1, }),
                        new BusinessService(new Uri("http://someuri"))))
                .AddMessageParserServices();
            _serviceProvider = _services.BuildServiceProvider();
        }
    }
}
