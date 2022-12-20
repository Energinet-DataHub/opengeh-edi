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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MediatR;
using Messaging.Api.Configuration.Middleware.Correlation;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.Infrastructure.Configuration.InternalCommands;
using Messaging.IntegrationTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Messaging.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly ServiceCollection _services;
        private readonly DatabaseFixture _databaseFixture;
        private IServiceProvider _serviceProvider;
        private bool _disposed;

        protected TestBase(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _databaseFixture.CleanupDatabase();

            Environment.SetEnvironmentVariable("FEATUREFLAG_ACTORMESSAGEQUEUE", "true");
            _services = new ServiceCollection();

            _services.AddSingleton(new EnergySupplyingServiceBusClientConfiguration("Fake"));
            _services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactoryStub>();

            _services.AddSingleton(
                _ => new ServiceBusClient(CreateFakeServiceBusConnectionString()));
            CompositionRoot.Initialize(_services)
                .AddAuthentication()
                .AddPeekConfiguration(new BundleConfigurationStub())
                .AddRemoteBusinessService<DummyRequest, DummyReply>(sp => new RemoteBusinessServiceRequestSenderSpy<DummyRequest>("Dummy"), "Dummy")
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
                .AddHttpClientAdapter(_ => new HttpClientSpy())
                .AddMoveInServices(
                    new MoveInSettings(new MessageDelivery(new GridOperator() { GracePeriodInDaysAfterEffectiveDateIfNotUpdated = 1, }), new BusinessService(new Uri("http://someuri"))))
                .AddMessageParserServices();
            _serviceProvider = _services.BuildServiceProvider();
        }

        public Task InvokeCommandAsync(object command)
        {
            return GetService<IMediator>().Send(command);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public T GetService<T>()
            where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            ((ServiceProvider)_serviceProvider).Dispose();
            _disposed = true;
        }

        protected void RegisterInstance<TService>(TService instance)
        where TService : class
        {
            _services.AddScoped(_ => instance);
            _serviceProvider = _services.BuildServiceProvider();
        }

        protected Task<TResult> InvokeCommandAsync<TResult>(ICommand<TResult> command)
        {
            return GetService<IMediator>().Send(command);
        }

        private static string CreateFakeServiceBusConnectionString()
        {
            return new StringBuilder()
                .Append(CultureInfo.InvariantCulture, $"Endpoint=sb://sb-{Guid.NewGuid():N}.servicebus.windows.net/;")
                .Append("SharedAccessKeyName=send;")
                .Append(CultureInfo.InvariantCulture, $"SharedAccessKey={Guid.NewGuid():N}")
                .ToString();
        }
    }
}
