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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.Core.TestCommon.Diagnostics;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Xunit.Abstractions;

namespace AcceptanceTest.Fixtures
{
    public class TestCommonHostFixture : IAsyncLifetime
    {
        public TestCommonHostFixture()
        {
            var web01BaseUrl = "http://localhost:5000";

            WholeSaleApiStubHost = WebHost.CreateDefaultBuilder()
                .UseStartup<WholeSaleApiStub.Startup>()
                .UseUrls(web01BaseUrl)
                .Build();

            TestLogger = new TestDiagnosticsLogger();

            AzuriteManager = new AzuriteManager();
            IntegrationTestConfiguration = new IntegrationTestConfiguration();
            ServiceBusResourceProvider = new ServiceBusResourceProvider(IntegrationTestConfiguration.ServiceBusConnectionString, TestLogger);

            HostConfigurationBuilder = new FunctionAppHostConfigurationBuilder();
        }

        public ITestDiagnosticsLogger TestLogger { get; }

        [NotNull]
        public FunctionAppHostManager? ApiHostManager { get; private set; }

        private AzuriteManager AzuriteManager { get; }

        private IntegrationTestConfiguration IntegrationTestConfiguration { get; }

        private ServiceBusResourceProvider ServiceBusResourceProvider { get; }

        private FunctionAppHostConfigurationBuilder HostConfigurationBuilder { get; }

        private IWebHost WholeSaleApiStubHost { get; }

        public async Task InitializeAsync()
        {
            // => Storage emulator
            AzuriteManager.StartAzurite();
            var port = 7070;
            var apiHostSettings = CreateAppHostSettings("Api", ref port);

            // => Integration events
            apiHostSettings.ProcessEnvironmentVariables.Add("INTEGRATIONEVENT_CONNECTION_STRING", ServiceBusResourceProvider.ConnectionString);

            await ServiceBusResourceProvider
                .BuildTopic("integrationevent-topic")
                    .Do(topicProperties =>
                    {
                        apiHostSettings.ProcessEnvironmentVariables.Add("INTEGRATIONEVENT_TOPIC_NAME", topicProperties.Name);
                    })
                .CreateAsync().ConfigureAwait(false);

            // => Create and start host's
            ApiHostManager = new FunctionAppHostManager(apiHostSettings, TestLogger);

            StartHost(ApiHostManager);
            await WholeSaleApiStubHost.StartAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Use this method to attach <paramref name="testOutputHelper"/> to the host logging pipeline.
        /// While attached, any entries written to host log pipeline will also be logged to xUnit test output.
        /// It is important that it is only attached while a test i active. Hence, it should be attached in
        /// the test class constructor; and detached in the test class Dispose method (using 'null').
        /// </summary>
        /// <param name="testOutputHelper">If a xUnit test is active, this should be the instance of xUnit's <see cref="ITestOutputHelper"/>; otherwise it should be 'null'.</param>
        public void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            TestLogger.TestOutputHelper = testOutputHelper;
        }

        public async Task DisposeAsync()
        {
            ApiHostManager.Dispose();
            AzuriteManager.Dispose();
            WholeSaleApiStubHost.Dispose();
            await ServiceBusResourceProvider.DisposeAsync().ConfigureAwait(false);
        }

        private static void StartHost(FunctionAppHostManager hostManager)
        {
            try
            {
                hostManager.StartHost();
            }
            catch (Exception)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                // Rethrow
                throw;
            }
        }

        private static string GetBuildConfiguration()
        {
#if DEBUG
            return "Debug";
#else
            return "Release";
#endif
        }

        private FunctionAppHostSettings CreateAppHostSettings(string csprojName, ref int port)
        {
            var buildConfiguration = GetBuildConfiguration();

            var appHostSettings = HostConfigurationBuilder.CreateFunctionAppHostSettings();
            appHostSettings.FunctionApplicationPath = $"..\\..\\..\\..\\{csprojName}\\bin\\{buildConfiguration}\\net6.0";
            appHostSettings.Port = ++port;

            appHostSettings.ProcessEnvironmentVariables.Add("AzureWebJobsStorage", "UseDevelopmentStorage=true");
            appHostSettings.ProcessEnvironmentVariables.Add("APPINSIGHTS_INSTRUMENTATIONKEY", IntegrationTestConfiguration.ApplicationInsightsInstrumentationKey);

            return appHostSettings;
        }
    }
}
