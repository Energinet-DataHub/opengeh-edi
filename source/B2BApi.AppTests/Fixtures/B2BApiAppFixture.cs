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

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Storage.Blobs;
using Energinet.DataHub.Core.App.Common.Extensions.Options;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.DurableFunctionApp.TestCommon.DurableTask;
using Energinet.DataHub.Core.FunctionApp.TestCommon.AppConfiguration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.Core.TestCommon.Diagnostics;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Energinet.DataHub.EDI.B2BApi.Functions;
using Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Database;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IntegrationTests.AuditLog.Fixture;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using Energinet.DataHub.RevisionLog.Integration.Options;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Xunit;
using Xunit.Abstractions;
using AmountsPerChargeViewSchemaDefinition = Energinet.DataHub.EDI.B2BApi.AppTests.TestData.CalculationResults.AmountsPerChargeViewSchemaDefinition;
using EnergyPerGaViewSchemaDefinition = Energinet.DataHub.EDI.B2BApi.AppTests.TestData.CalculationResults.EnergyPerGaViewSchemaDefinition;
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

    private const string DatabricksCatalogName = "hive_metastore";

    private readonly DeltaTableOptions _calculationResultsDatabricksOptions;

    public B2BApiAppFixture()
    {
        var constructorStopwatch = Stopwatch.StartNew();
        var stopwatch = Stopwatch.StartNew();

        TestLogger = new TestDiagnosticsLogger();
        LogStopwatch(stopwatch, nameof(TestLogger));

        IntegrationTestConfiguration = new IntegrationTestConfiguration();
        LogStopwatch(stopwatch, nameof(IntegrationTestConfiguration));

        AzuriteManager = new AzuriteManager(useOAuth: true);
        AzuriteManager.CleanupAzuriteStorage();
        LogStopwatch(stopwatch, nameof(AzuriteManager));

        DurableTaskManager = new DurableTaskManager(
            "OrchestrationsStorageConnectionString",
            AzuriteManager.FullConnectionString);
        LogStopwatch(stopwatch, nameof(DurableTaskManager));

        DatabaseManager = new EdiDatabaseManager("B2BApiAppTests");
        LogStopwatch(stopwatch, nameof(DatabaseManager));

        ServiceBusResourceProvider = new ServiceBusResourceProvider(
            TestLogger,
            IntegrationTestConfiguration.ServiceBusFullyQualifiedNamespace,
            IntegrationTestConfiguration.Credential);
        LogStopwatch(stopwatch, nameof(ServiceBusResourceProvider));

        ServiceBusListenerMock = new ServiceBusListenerMock(
            TestLogger,
            IntegrationTestConfiguration.ServiceBusFullyQualifiedNamespace,
            IntegrationTestConfiguration.Credential);
        LogStopwatch(stopwatch, nameof(ServiceBusListenerMock));

        HostConfigurationBuilder = new FunctionAppHostConfigurationBuilder();
        LogStopwatch(stopwatch, nameof(HostConfigurationBuilder));

        EdiDatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            IntegrationTestConfiguration.DatabricksSettings,
            "B2BApi_tests_edi");
        LogStopwatch(stopwatch, nameof(EdiDatabricksSchemaManager));

        CalculationResultsDatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            IntegrationTestConfiguration.DatabricksSettings,
            "B2BApi_tests_calculation_results");
        LogStopwatch(stopwatch, nameof(CalculationResultsDatabricksSchemaManager));

        _calculationResultsDatabricksOptions = new DeltaTableOptions
        {
            DatabricksCatalogName = DatabricksCatalogName,
            WholesaleCalculationResultsSchemaName = CalculationResultsDatabricksSchemaManager.SchemaName,
        };

        AuditLogMockServer = new AuditLogMockServer();
        LogStopwatch(stopwatch, nameof(AuditLogMockServer));

        LogStopwatch(constructorStopwatch, "B2BApiAppFixture constructor");
    }

    public AuditLogMockServer AuditLogMockServer { get; }

    public ITestDiagnosticsLogger TestLogger { get; }

    [NotNull]
    public FunctionAppHostManager? AppHostManager { get; private set; }

    [NotNull]
    public IDurableClient? DurableClient { get; private set; }

    /// <summary>
    /// Topic resource for integration events.
    /// </summary>
    [NotNull]
    public TopicResource? IntegrationEventsTopicResource { get; private set; }

    [NotNull]
    public TopicResource? ProcessManagerStartTopicResource { get; private set; }

    [NotNull]
    public TopicResource? ProcessManagerNotifyTopicResource { get; private set; }

    [NotNull]
    public TopicResource? EdiTopicResource { get; private set; }

    public TopicResource MeasurementsSyncTopicResource { get; private set; } = null!;

    public ServiceBusListenerMock ServiceBusListenerMock { get; }

    public DatabricksSchemaManager EdiDatabricksSchemaManager { get; }

    public DatabricksSchemaManager CalculationResultsDatabricksSchemaManager { get; }

    public EdiDatabaseManager DatabaseManager { get; }

    private IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    private AzuriteManager AzuriteManager { get; }

    private DurableTaskManager DurableTaskManager { get; }

    private ServiceBusResourceProvider ServiceBusResourceProvider { get; }

    private FunctionAppHostConfigurationBuilder HostConfigurationBuilder { get; }

    public async Task InitializeAsync()
    {
        var initializeStopwatch = Stopwatch.StartNew();
        var stopwatch = Stopwatch.StartNew();

        // Storage emulator
        AzuriteManager.StartAzurite();
        LogStopwatch(stopwatch, nameof(AzuriteManager.StartAzurite));

        CreateRequiredContainers();
        LogStopwatch(stopwatch, nameof(CreateRequiredContainers));

        // Database
        await DatabaseManager.CreateDatabaseAsync();
        LogStopwatch(stopwatch, nameof(DatabaseManager.CreateDatabaseAsync));

        // Prepare host settings
        var port = 8000;
        var appHostSettings = CreateAppHostSettings("B2BApi", ref port);
        LogStopwatch(stopwatch, nameof(CreateAppHostSettings));

        // ServiceBus entities
        IntegrationEventsTopicResource = await ServiceBusResourceProvider
            .BuildTopic("integration-events")
            .Do(topic => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IntegrationEventsOptions.SectionName}__{nameof(IntegrationEventsOptions.TopicName)}", topic.Name))
            .AddSubscription("subscription")
            .Do(subscription => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IntegrationEventsOptions.SectionName}__{nameof(IntegrationEventsOptions.SubscriptionName)}", subscription.SubscriptionName))
            .CreateAsync();
        LogStopwatch(stopwatch, nameof(IntegrationEventsTopicResource));

        ProcessManagerStartTopicResource = await ServiceBusResourceProvider
            .BuildTopic("process-manager-start")
            .Do(topic => appHostSettings.ProcessEnvironmentVariables
                .Add($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.StartTopicName)}", topic.Name))
            .Do(topic => appHostSettings.ProcessEnvironmentVariables // TODO: Do we need a separate topic for tests as well?
                .Add($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.Brs021ForwardMeteredDataStartTopicName)}", topic.Name))
            .AddSubscription("process-manager-subscription")
            .CreateAsync();
        LogStopwatch(stopwatch, nameof(ProcessManagerStartTopicResource));
        await ServiceBusListenerMock.AddTopicSubscriptionListenerAsync(
            topicName: ProcessManagerStartTopicResource.Name,
            subscriptionName: ProcessManagerStartTopicResource.Subscriptions.Single().SubscriptionName);

        ProcessManagerNotifyTopicResource = await ServiceBusResourceProvider
            .BuildTopic("process-manager-notify")
            .Do(topic => appHostSettings.ProcessEnvironmentVariables
                .Add($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.NotifyTopicName)}", topic.Name))
            .Do(topic => appHostSettings.ProcessEnvironmentVariables // TODO: Do we need a separate topic for tests as well?
                .Add($"{ProcessManagerServiceBusClientOptions.SectionName}__{nameof(ProcessManagerServiceBusClientOptions.Brs021ForwardMeteredDataNotifyTopicName)}", topic.Name))
            .AddSubscription("process-manager-subscription")
            .CreateAsync();
        LogStopwatch(stopwatch, nameof(ProcessManagerNotifyTopicResource));
        await ServiceBusListenerMock.AddTopicSubscriptionListenerAsync(
            topicName: ProcessManagerNotifyTopicResource.Name,
            subscriptionName: ProcessManagerNotifyTopicResource.Subscriptions.Single().SubscriptionName);

        await ServiceBusResourceProvider
            .BuildQueue("incoming-messages")
            .Do(queue => appHostSettings.ProcessEnvironmentVariables
                .Add($"{IncomingMessagesOptions.SectionName}__{nameof(IncomingMessagesOptions.QueueName)}", queue.Name))
            .CreateAsync();
        LogStopwatch(stopwatch, "ServiceBusQueue (incoming-messages)");

        EdiTopicResource = await ServiceBusResourceProvider
            .BuildTopic("edi")
                .Do(topic => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.Name)}", topic.Name))
            .AddSubscription("enqueue-brs-023-027-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_023_027.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_023_027_SubscriptionName)}", s.SubscriptionName))
            .AddSubscription("enqueue-brs-026-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_026.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_026_SubscriptionName)}", s.SubscriptionName))
            .AddSubscription("enqueue-brs-028-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_028.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_028_SubscriptionName)}", s.SubscriptionName))
            .AddSubscription("enqueue-brs-021-forward-metered-data-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_021_ForwardedMeteredData.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_021_Forward_Metered_Data_SubscriptionName)}", s.SubscriptionName))
            .AddSubscription("enqueue-brs-024-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_024.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_024_SubscriptionName)}", s.SubscriptionName))
            .AddSubscription("enqueue-brs-025-subscription")
                .AddSubjectFilter(EnqueueActorMessagesV1.BuildServiceBusMessageSubject(Brs_025.V1))
                .Do(s => appHostSettings.ProcessEnvironmentVariables
                    .Add($"{EdiTopicOptions.SectionName}__{nameof(EdiTopicOptions.EnqueueBrs_025_SubscriptionName)}", s.SubscriptionName))
            .CreateAsync();

        MeasurementsSyncTopicResource = await ServiceBusResourceProvider
            .BuildTopic("migration")
            .Do(topic => appHostSettings.ProcessEnvironmentVariables
                .Add($"{MeasurementsSynchronizationOptions.SectionName}__{nameof(MeasurementsSynchronizationOptions.TopicName)}", topic.Name))
            .AddSubscription("subscription")
            .Do(subscription => appHostSettings.ProcessEnvironmentVariables
                .Add($"{MeasurementsSynchronizationOptions.SectionName}__{nameof(MeasurementsSynchronizationOptions.TimeSeriesSync_SubscriptionName)}", subscription.SubscriptionName))
            .CreateAsync();

        LogStopwatch(stopwatch, nameof(IntegrationEventsTopicResource));

        LogStopwatch(stopwatch, nameof(ServiceBusListenerMock.AddQueueListenerAsync));

        AuditLogMockServer.StartServer();
        LogStopwatch(stopwatch, nameof(AuditLogMockServer.StartServer));

        // Create and start host
        AppHostManager = new FunctionAppHostManager(appHostSettings, TestLogger);
        LogStopwatch(stopwatch, nameof(AppHostManager));

        StartHost(AppHostManager);
        LogStopwatch(stopwatch, nameof(StartHost));

        // Create durable client when TaskHub has been created
        DurableClient = DurableTaskManager.CreateClient(taskHubName: TaskHubName);
        LogStopwatch(stopwatch, nameof(DurableTaskManager.CreateClient));

        await CalculationResultsDatabricksSchemaManager.CreateSchemaAsync();
        LogStopwatch(
            stopwatch,
            $"{nameof(CalculationResultsDatabricksSchemaManager)}.{nameof(CalculationResultsDatabricksSchemaManager.CreateSchemaAsync)}");

        await CreateCalculationResultDatabricksDataAsync();
        LogStopwatch(stopwatch, nameof(CreateCalculationResultDatabricksDataAsync));

        await CreateEnergyCalculationResultDatabricksDataAsync();
        LogStopwatch(stopwatch, nameof(CreateEnergyCalculationResultDatabricksDataAsync));

        LogStopwatch(initializeStopwatch, nameof(InitializeAsync));
    }

    public async Task DisposeAsync()
    {
        AppHostManager.Dispose();
        AzuriteManager.Dispose();
        await CalculationResultsDatabricksSchemaManager.DropSchemaAsync();
        await DurableTaskManager.DisposeAsync();
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
        List<KeyValuePair<string, bool>> featureFlags)
    {
        AppHostManager.RestartHostIfChanges(
            featureFlags.ToDictionary(
                keySelector: (element) => $"FeatureManagement__{element.Key}",
                elementSelector: (element) => element.Value.ToString().ToLower()));
    }

    public string CreateSubsystemToken(string applicationIdUri = SubsystemAuthenticationOptionsForTests.ApplicationIdUri)
    {
        var tokenResponse = IntegrationTestConfiguration.Credential.GetToken(
            new TokenRequestContext([applicationIdUri]), CancellationToken.None);

        return tokenResponse.Token;
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
        appHostSettings.FunctionApplicationPath = $"..\\..\\..\\..\\{csprojName}\\bin\\{buildConfiguration}\\net9.0";
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

        // Logging
        appHostSettings.ProcessEnvironmentVariables.Add(
            "Logging__LogLevel__Default",
            "Information");
        // => Disable extensive logging from EF Core
        appHostSettings.ProcessEnvironmentVariables.Add(
            "Logging__LogLevel__Microsoft.EntityFrameworkCore",
            "Warning");
        // => Disable extensive logging when using Azure Storage
        appHostSettings.ProcessEnvironmentVariables.Add(
            "Logging__LogLevel__Azure.Core",
            "Error");

        // Durable Functions
        // => Task Hub Name
        appHostSettings.ProcessEnvironmentVariables.Add(
            "OrchestrationsTaskHubName",
            TaskHubName);
        // => Task Hub Storage account connection string
        appHostSettings.ProcessEnvironmentVariables.Add(
            "OrchestrationsStorageConnectionString",
            AzuriteManager.FullConnectionString);

        // Make Orchestrator poll for updates every second (default is every 30 seconds) by overriding maxQueuePollingInterval
        // (ref: https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-bindings?tabs=python-v2%2Cisolated-process%2C2x-durable-functions&pivots=programming-language-csharp#hostjson-settings)
        appHostSettings.ProcessEnvironmentVariables.Add(
            "AzureFunctionsJobHost__extensions__durableTask__storageProvider__maxQueuePollingInterval",
            "00:00:01");

        // Document storage
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{BlobServiceClientConnectionOptions.SectionName}__{nameof(BlobServiceClientConnectionOptions.StorageAccountUrl)}",
            AzuriteManager.BlobStorageServiceUri.AbsoluteUri);

        // Dead-letter logging
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{BlobDeadLetterLoggerOptions.SectionName}__{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}",
            AzuriteManager.BlobStorageServiceUri.OriginalString);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{BlobDeadLetterLoggerOptions.SectionName}__{nameof(BlobDeadLetterLoggerOptions.ContainerName)}",
            "edi-b2bapi");

        // Database
        var dbConnectionString = DatabaseManager.ConnectionString;
        if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate might be required for some
            dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";
        appHostSettings.ProcessEnvironmentVariables.Add(
            "DB_CONNECTION_STRING",
            dbConnectionString);

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
            $"{EdiDatabricksOptions.SectionName}__{nameof(EdiDatabricksOptions.DatabaseName)}",
            EdiDatabricksSchemaManager.SchemaName);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{EdiDatabricksOptions.SectionName}__{nameof(EdiDatabricksOptions.CatalogName)}",
            DatabricksCatalogName);

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{nameof(DeltaTableOptions.WholesaleCalculationResultsSchemaName)}",
            _calculationResultsDatabricksOptions.WholesaleCalculationResultsSchemaName);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{nameof(DeltaTableOptions.DatabricksCatalogName)}",
            _calculationResultsDatabricksOptions.DatabricksCatalogName);

        // ServiceBus connection strings
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{ServiceBusNamespaceOptions.SectionName}__{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}",
            ServiceBusResourceProvider.FullyQualifiedNamespace);

        // Feature Flags: Default values
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{FeatureFlagNames.SectionName}__{FeatureFlagNames.UsePeekMessages}",
            true.ToString().ToLower());

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{FeatureFlagNames.SectionName}__{FeatureFlagNames.PM25CIM}",
            true.ToString().ToLower());

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{FeatureFlagNames.SectionName}__{FeatureFlagNames.PM25Ebix}",
            true.ToString().ToLower());

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"RevisionLogOptions__{nameof(RevisionLogOptions.ApiAddress)}",
            AuditLogMockServer.IngestionUrl);

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"AzureWebJobs.TenSecondsHasPassed.Disabled",
            "true");
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"AzureWebJobs.ADayHasPassed.Disabled",
            "true");
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"AzureWebJobs.{nameof(OutboxPublisher)}.Disabled",
            "true");
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"AzureWebJobs.{nameof(OutgoingMessagesBundler)}.Disabled",
            "true");

        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{FeatureFlagNames.SectionName}__{FeatureFlagNames.SyncMeasurements}",
            true.ToString().ToLower());

        // Bundling
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{BundlingOptions.SectionName}__{nameof(BundlingOptions.BundleMessagesOlderThanSeconds)}",
            "0"); // Setting the "bundle messages older than" to 0 ensures that bundles will be created for outgoing messages as soon as the function is triggered

        // Feature Management => Azure App Configuration settings
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{AzureAppConfigurationOptions.SectionName}__{nameof(AzureAppConfigurationOptions.Endpoint)}",
            IntegrationTestConfiguration.AppConfigurationEndpoint);
        appHostSettings.ProcessEnvironmentVariables.Add(
            AppConfigurationManager.DisableProviderSettingName,
            "true");

        // Subsystem authentication
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{SubsystemAuthenticationOptions.SectionName}__{nameof(SubsystemAuthenticationOptions.ApplicationIdUri)}",
            SubsystemAuthenticationOptionsForTests.ApplicationIdUri);
        appHostSettings.ProcessEnvironmentVariables.Add(
            $"{SubsystemAuthenticationOptions.SectionName}__{nameof(SubsystemAuthenticationOptions.Issuer)}",
            SubsystemAuthenticationOptionsForTests.Issuer);
        return appHostSettings;
    }

    private async Task CreateCalculationResultDatabricksDataAsync()
    {
        await CalculationResultsDatabricksSchemaManager.CreateTableAsync(
            _calculationResultsDatabricksOptions.AMOUNTS_PER_CHARGE_V1_VIEW_NAME,
            AmountsPerChargeViewSchemaDefinition.SchemaDefinition);

        const string amountsPerChargeFileName = "wholesale_calculation_results.amounts_per_charge_v1.csv";
        var amountsPerChargeFilePath = Path.Combine("TestData", "CalculationResults", amountsPerChargeFileName);
        await CalculationResultsDatabricksSchemaManager.InsertFromCsvFileAsync(
            _calculationResultsDatabricksOptions.AMOUNTS_PER_CHARGE_V1_VIEW_NAME,
            AmountsPerChargeViewSchemaDefinition.SchemaDefinition,
            amountsPerChargeFilePath);
    }

    private async Task CreateEnergyCalculationResultDatabricksDataAsync()
    {
        await CalculationResultsDatabricksSchemaManager.CreateTableAsync(
            _calculationResultsDatabricksOptions.ENERGY_V1_VIEW_NAME,
            EnergyPerGaViewSchemaDefinition.SchemaDefinition);

        const string energyPerGaFileName = "wholesale_calculation_results.energy_per_ga_v1.csv";
        var energyPerGaFilePath = Path.Combine("TestData", "CalculationResults", energyPerGaFileName);
        await CalculationResultsDatabricksSchemaManager.InsertFromCsvFileAsync(
            _calculationResultsDatabricksOptions.ENERGY_V1_VIEW_NAME,
            EnergyPerGaViewSchemaDefinition.SchemaDefinition,
            energyPerGaFilePath);
    }

    private void LogStopwatch(Stopwatch stopwatch, string tag)
    {
        var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
        TestLogger.WriteLine($"[PERFORMANCE][{elapsedSeconds:00.00}s] {tag} took {elapsedSeconds:N1} seconds");
        stopwatch.Restart();
    }
}
