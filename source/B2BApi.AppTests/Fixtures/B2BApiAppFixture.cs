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
using Azure.Storage.Blobs;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.Core.TestCommon.Diagnostics;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Database;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Xunit;
using Xunit.Abstractions;
using HttpClientFactory = Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks.HttpClientFactory;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;

/// <summary>
/// Support testing B2BApi app.
/// </summary>
public class B2BApiAppFixture : IAsyncLifetime
{
    /// <summary>
    /// Durable Functions Task Hub Name
    /// See naming constraints: https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-task-hubs?tabs=csharp#task-hub-names
    /// </summary>
    private const string TaskHubName = "EdiTest01";

    public B2BApiAppFixture()
    {
        var constructorStopwatch = new Stopwatch();
        var stopwatch = new Stopwatch();

        constructorStopwatch.Start();
        stopwatch.Start();

        TestLogger = new TestDiagnosticsLogger();
        LogTimestamp(stopwatch, nameof(TestLogger));

        IntegrationTestConfiguration = new IntegrationTestConfiguration();
        LogTimestamp(stopwatch, nameof(IntegrationTestConfiguration));

        AzuriteManager = new AzuriteManager(useOAuth: false);
        LogTimestamp(stopwatch, nameof(AzuriteManager));

        CleanupAzuriteStorage();
        LogTimestamp(stopwatch, nameof(CleanupAzuriteStorage));

        DurableTaskManager = new DurableTaskManager(
            "AzureWebJobsStorage",
            AzuriteManager.FullConnectionString);
        LogTimestamp(stopwatch, nameof(DurableTaskManager));

        DatabaseManager = new EdiDatabaseManager();
        LogTimestamp(stopwatch, nameof(DatabaseManager));

        ServiceBusResourceProvider = new ServiceBusResourceProvider(
            IntegrationTestConfiguration.ServiceBusConnectionString,
            TestLogger);
        LogTimestamp(stopwatch, nameof(ServiceBusResourceProvider));

        ServiceBusListenerMock = new ServiceBusListenerMock(
            IntegrationTestConfiguration.ServiceBusConnectionString,
            TestLogger);
        LogTimestamp(stopwatch, nameof(ServiceBusListenerMock));

        HostConfigurationBuilder = new FunctionAppHostConfigurationBuilder();
        LogTimestamp(stopwatch, nameof(HostConfigurationBuilder));

        DatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            IntegrationTestConfiguration.DatabricksSettings,
            "edi_B2BApi_tests");

        LogTimestamp(stopwatch, nameof(DatabricksSchemaManager));
        LogTimestamp(constructorStopwatch, "B2BApiAppFixture constructor");
    }

    public ITestDiagnosticsLogger TestLogger { get; }

    [NotNull]
    public FunctionAppHostManager? AppHostManager { get; private set; }

    [NotNull]
    public IDurableClient? DurableClient { get; private set; }

    /// <summary>
    /// Topic resource for integration events.
    /// </summary>
    [NotNull]
    public TopicResource? TopicResource { get; private set; }

    public ServiceBusListenerMock ServiceBusListenerMock { get; }

    public DatabricksSchemaManager DatabricksSchemaManager { get; }

    private IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    private AzuriteManager AzuriteManager { get; }

    private DurableTaskManager DurableTaskManager { get; }

    private EdiDatabaseManager DatabaseManager { get; }

    private ServiceBusResourceProvider ServiceBusResourceProvider { get; }

    private FunctionAppHostConfigurationBuilder HostConfigurationBuilder { get; }

    public async Task InitializeAsync()
    {
        var initializeStopwatch = new Stopwatch();
        var stopwatch = new Stopwatch();

        // Storage emulator
        AzuriteManager.StartAzurite();
        LogTimestamp(stopwatch, nameof(AzuriteManager.StartAzurite));

        CreateRequiredContainers();
        LogTimestamp(stopwatch, nameof(CreateRequiredContainers));

        // Database
        await DatabaseManager.CreateDatabaseAsync();
        LogTimestamp(stopwatch, nameof(DatabaseManager.CreateDatabaseAsync));

        // Prepare host settings
        var port = 8000;
        var appHostSettings = CreateAppHostSettings("B2BApi", ref port);
        LogTimestamp(stopwatch, nameof(CreateAppHostSettings));

        // ServiceBus entities
        TopicResource = await ServiceBusResourceProvider
            .BuildTopic("integration-events")
            .Do(topic => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IntegrationEventsOptions.SectionName}__{nameof(IntegrationEventsOptions.TopicName)}", topic.Name))
            .AddSubscription("subscription")
            .Do(subscription => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IntegrationEventsOptions.SectionName}__{nameof(IntegrationEventsOptions.SubscriptionName)}", subscription.SubscriptionName))
            .CreateAsync();
        LogTimestamp(stopwatch, nameof(TopicResource));

        await ServiceBusResourceProvider
            .BuildQueue("edi-inbox")
            .Do(queue => appHostSettings.ProcessEnvironmentVariables
                .Add($"{EdiInboxOptions.SectionName}__{nameof(EdiInboxOptions.QueueName)}", queue.Name))
            .CreateAsync();
        LogTimestamp(stopwatch, "service bus queue (edi-inbox)");

        var wholesaleInboxQueueResource = await ServiceBusResourceProvider
            .BuildQueue("wholesale-inbox")
            .Do(queue => appHostSettings.ProcessEnvironmentVariables
                .Add($"{WholesaleInboxOptions.SectionName}__{nameof(WholesaleInboxOptions.QueueName)}", queue.Name))
            .CreateAsync();
        LogTimestamp(stopwatch, "service bus queue (wholesale-inbox)");

        await ServiceBusResourceProvider
            .BuildQueue("incoming-messages")
            .Do(queue => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IncomingMessagesQueueOptions.SectionName}__{nameof(IncomingMessagesQueueOptions.QueueName)}", queue.Name))
            .CreateAsync();
        LogTimestamp(stopwatch, "service bus queue incoming-messages");

        // => Receive messages on Wholesale Inbox Queue
        await ServiceBusListenerMock.AddQueueListenerAsync(wholesaleInboxQueueResource.Name);
        LogTimestamp(stopwatch, nameof(ServiceBusListenerMock.AddQueueListenerAsync));

        // Create and start host
        AppHostManager = new FunctionAppHostManager(appHostSettings, TestLogger);
        LogTimestamp(stopwatch, nameof(AppHostManager));

        StartHost(AppHostManager);
        LogTimestamp(stopwatch, nameof(StartHost));

        // Create durable client when TaskHub has been created
        DurableClient = DurableTaskManager.CreateClient(taskHubName: TaskHubName);
        LogTimestamp(stopwatch, nameof(DurableTaskManager.CreateClient));

        LogTimestamp(initializeStopwatch, nameof(InitializeAsync));
    }

    public async Task DisposeAsync()
    {
        AppHostManager.Dispose();
        DurableTaskManager.Dispose();
        AzuriteManager.Dispose();
        await ServiceBusResourceProvider.DisposeAsync();
        await DatabaseManager.DeleteDatabaseAsync();
    }

    /// <summary>
    /// Use this method to attach <paramref name="testOutputHelper"/> to the host logging pipeline.
    /// While attached, any entries written to host log pipeline will also be logged to xUnit test output.
    /// It is important that it is only attached while a test i active. Hence, it should be attached in
    /// the test class constructor; and detached in the test class Dispose method (using 'null').
    /// </summary>
    /// <param name="testOutputHelper">If a xUnit test is active, this should be the instance of xUnit's <see cref="ITestOutputHelper"/>;
    /// otherwise it should be 'null'.</param>
    public void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        TestLogger.TestOutputHelper = testOutputHelper;
    }

    public void EnsureAppHostUsesFeatureFlagValue(
        bool enableCalculationCompletedEvent,
        bool enableCalculationCompletedEventForBalanceFixing,
        bool enableCalculationCompletedEventForWholesaleFixing)
    {
        AppHostManager.RestartHostIfChanges(new Dictionary<string, string>
        {
            { "FeatureManagement__UseCalculationCompletedEvent", enableCalculationCompletedEvent.ToString().ToLower() },
            { "FeatureManagement__UseCalculationCompletedEventForBalanceFixing", enableCalculationCompletedEventForBalanceFixing.ToString().ToLower() },
            { "FeatureManagement__UseCalculationCompletedEventForWholesaleFixing", enableCalculationCompletedEventForWholesaleFixing.ToString().ToLower() },
        });
    }

    private static void StartHost(FunctionAppHostManager hostManager)
    {
        IEnumerable<string> hostStartupLog;

        try
        {
            hostManager.StartHost();
        }
        catch (Exception)
        {
            // Function App Host failed during startup.
            // Exception has already been logged by host manager.
            hostStartupLog = hostManager.GetHostLogSnapshot();

            if (Debugger.IsAttached)
                Debugger.Break();

            // Rethrow
            throw;
        }

        // Function App Host started.
        hostStartupLog = hostManager.GetHostLogSnapshot();
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
        return "Release";
#endif
    }

    /// <summary>
    /// Cleanup Azurite storage to avoid situations where Durable Functions
    /// would otherwise continue working on old orchestrations that e.g. failed in
    /// previous runs.
    /// </summary>
    private void CleanupAzuriteStorage()
    {
        if (Directory.Exists("__blobstorage__"))
            Directory.Delete("__blobstorage__", true);

        if (Directory.Exists("__queuestorage__"))
            Directory.Delete("__queuestorage__", true);

        if (Directory.Exists("__tablestorage__"))
            Directory.Delete("__tablestorage__", true);

        if (File.Exists("__azurite_db_blob__.json"))
            File.Delete("__azurite_db_blob__.json");

        if (File.Exists("__azurite_db_blob_extent__.json"))
            File.Delete("__azurite_db_blob_extent__.json");

        if (File.Exists("__azurite_db_queue__.json"))
            File.Delete("__azurite_db_queue__.json");

        if (File.Exists("__azurite_db_queue_extent__.json"))
            File.Delete("__azurite_db_queue_extent__.json");

        if (File.Exists("__azurite_db_table__.json"))
            File.Delete("__azurite_db_table__.json");

        if (File.Exists("__azurite_db_table_extent__.json"))
            File.Delete("__azurite_db_table_extent__.json");
    }

    private void CreateRequiredContainers()
    {
        List<FileStorageCategory> containerCategories = [
            FileStorageCategory.ArchivedMessage(),
            FileStorageCategory.OutgoingMessage(),
        ];

        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);
        foreach (var fileStorageCategory in containerCategories)
        {
            var container = blobServiceClient.GetBlobContainerClient(fileStorageCategory.Value);
            var containerExists = container.Exists();

            if (!containerExists)
                container.Create();
        }
    }

    private FunctionAppHostSettings CreateAppHostSettings(string csprojName, ref int port)
    {
        var buildConfiguration = GetBuildConfiguration();

        var appHostSettings = HostConfigurationBuilder.CreateFunctionAppHostSettings();
        appHostSettings.FunctionApplicationPath = $"..\\..\\..\\..\\{csprojName}\\bin\\{buildConfiguration}\\net8.0";
        appHostSettings.Port = ++port;

        // It seems the host + worker is not ready if we use the default startup log message, so we override it here
        appHostSettings.HostStartedEvent = "Host lock lease acquired";

        appHostSettings.ProcessEnvironmentVariables.Add(
            "FUNCTIONS_WORKER_RUNTIME",
            "dotnet-isolated");
        appHostSettings.ProcessEnvironmentVariables.Add(
            "AzureWebJobsStorage",
            AzuriteManager.FullConnectionString);
        appHostSettings.ProcessEnvironmentVariables.Add(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            IntegrationTestConfiguration.ApplicationInsightsConnectionString);

        // Durable Functions Task Hub Name
        appHostSettings.ProcessEnvironmentVariables.Add(
            "OrchestrationsTaskHubName",
            TaskHubName);

        // Document storage
        appHostSettings.ProcessEnvironmentVariables.Add(
            nameof(BlobServiceClientConnectionOptions.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING),
            AzuriteManager.FullConnectionString);

        // Database
        appHostSettings.ProcessEnvironmentVariables.Add(
            "DB_CONNECTION_STRING",
            DatabaseManager.ConnectionString);

        // Databricks
        appHostSettings.ProcessEnvironmentVariables.Add(
            nameof(DatabricksSqlStatementOptions.WorkspaceUrl),
            IntegrationTestConfiguration.DatabricksSettings.WorkspaceUrl);
        appHostSettings.ProcessEnvironmentVariables.Add(
            nameof(DatabricksSqlStatementOptions.WorkspaceToken),
            IntegrationTestConfiguration.DatabricksSettings.WorkspaceAccessToken);
        appHostSettings.ProcessEnvironmentVariables.Add(
            nameof(DatabricksSqlStatementOptions.WarehouseId),
            IntegrationTestConfiguration.DatabricksSettings.WarehouseId);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{EdiDatabricksOptions.SectionName}:{nameof(EdiDatabricksOptions.DatabaseName)}",
            DatabricksSchemaManager.SchemaName);

        // ServiceBus connection strings
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.ManageConnectionString)}",
            ServiceBusResourceProvider.ConnectionString);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.ListenConnectionString)}",
            ServiceBusResourceProvider.ConnectionString);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{ServiceBusOptions.SectionName}__{nameof(ServiceBusOptions.SendConnectionString)}",
            ServiceBusResourceProvider.ConnectionString);

        // Feature Flags: Default values
        appHostSettings.ProcessEnvironmentVariables.Add(
            "FeatureManagement__UseCalculationCompletedEvent",
            true.ToString().ToLower());

        return appHostSettings;
    }

    private void LogTimestamp(Stopwatch stopwatch, string tag)
    {
        TestLogger.WriteLine($"[{stopwatch.ElapsedMilliseconds} ms] {tag}");
        stopwatch.Restart();
    }
}
