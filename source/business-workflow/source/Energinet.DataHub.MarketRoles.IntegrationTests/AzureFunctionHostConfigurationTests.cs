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
using Energinet.DataHub.MarketRoles.EntryPoints.Outbox;
using SimpleInjector;
using Xunit;

namespace Energinet.DataHub.MarketRoles.IntegrationTests
{
    public class AzureFunctionHostConfigurationTests
    {
        [Fact]
        public void OutboxHostConfigurationTest()
        {
            Environment.SetEnvironmentVariable("SHARED_SERVICEBUS_INTEGRATION_EVENT_CONNECTIONSTRING_TODO", "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=sender;SharedAccessKey=0XLJDfVlg+CorvdniMfp5S+SKbAeB9Kkiee6ZVBJJ4c=");
            Environment.SetEnvironmentVariable("MARKETROLES_DB_CONNECTION_STRING", "test");
            Environment.SetEnvironmentVariable("CONSUMER_REGISTERED_TOPIC_TODO", "test");
            var container = new Container();
            var program = new Program(container);

            program.ConfigureApplication();

            container.Verify();
        }
    }
}
