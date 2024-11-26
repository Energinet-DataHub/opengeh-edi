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
using Azure.Identity;
using Azure.Messaging.ServiceBus.Administration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;
using Energinet.DataHub.ProcessManager.Orchestrations.Tests.Fixtures;
using Energinet.DataHub.ProcessManager.Tests.Fixtures;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;

public class ProcessManagerClientFixture : IAsyncLifetime
{
    public ProcessManagerClientFixture()
    {
        var taskHubName = "ClientsTest01";

        DatabaseManager = new ProcessManagerDatabaseManager("ProcessManagerClientTests");

        IntegrationTestConfiguration = new IntegrationTestConfiguration();

        AzuriteManager = new AzuriteManager(useOAuth: true);

        OrchestrationsAppManager = new OrchestrationsAppManager(
            DatabaseManager,
            IntegrationTestConfiguration,
            AzuriteManager,
            taskHubName,
            8101,
            false);
        ProcessManagerAppManager = new ProcessManagerAppManager(
            DatabaseManager,
            IntegrationTestConfiguration,
            AzuriteManager,
            taskHubName,
            8102,
            false);

        ServiceBusResourceProvider = new ServiceBusResourceProvider(
            OrchestrationsAppManager.TestLogger,
            IntegrationTestConfiguration.ServiceBusFullyQualifiedNamespace,
            IntegrationTestConfiguration.Credential);
    }

    public AzuriteManager AzuriteManager { get; }

    public ServiceBusResourceProvider ServiceBusResourceProvider { get; }

    public IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    public ProcessManagerDatabaseManager DatabaseManager { get; }

    public OrchestrationsAppManager OrchestrationsAppManager { get; }

    public ProcessManagerAppManager ProcessManagerAppManager { get; }

    [NotNull]
    public SubscriptionProperties? Brs026Subscription { get; private set; }

    public async Task InitializeAsync()
    {
        OrchestrationsAppManager.CleanupAzuriteStorage();
        AzuriteManager.StartAzurite();

        var topicResource = await ServiceBusResourceProvider.BuildTopic("pm-topic")
            .AddSubscription("brs-026-subscription")
            .CreateAsync();
        Brs026Subscription = topicResource.Subscriptions.Single();

        await OrchestrationsAppManager.StartAsync(Brs026Subscription);
        await ProcessManagerAppManager.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await OrchestrationsAppManager.DisposeAsync();
        await ProcessManagerAppManager.DisposeAsync();
        await ServiceBusResourceProvider.DisposeAsync();
        AzuriteManager.Dispose();
    }

    public void SetTestOutputHelper(ITestOutputHelper? testOutputHelper)
    {
        OrchestrationsAppManager.SetTestOutputHelper(testOutputHelper);
        ProcessManagerAppManager.SetTestOutputHelper(testOutputHelper);
    }
}
