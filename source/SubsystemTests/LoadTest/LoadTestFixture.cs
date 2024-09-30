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

using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;

namespace Energinet.DataHub.EDI.SubsystemTests.LoadTest;

// ReSharper disable once ClassNeverInstantiated.Global -- Instantiated by xUnit
public sealed class LoadTestFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly DurableTaskManager _durableTaskManager;
    private readonly ServiceBusClient _serviceBusClient;

    public LoadTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("subsystemtests.dev002.settings.json", true)
            .AddEnvironmentVariables();

        var baseConfiguration = configurationBuilder.Build();
        var sharedKeyVaultName = GetConfigurationValue<string>(baseConfiguration, "SHARED_KEYVAULT_NAME");
        var ediKeyVaultName = GetConfigurationValue<string>(baseConfiguration, "INTERNAL_KEYVAULT_NAME");

        configurationBuilder = configurationBuilder
            .AddAuthenticatedAzureKeyVault($"https://{sharedKeyVaultName}.vault.azure.net/")
            .AddAuthenticatedAzureKeyVault($"https://{ediKeyVaultName}.vault.azure.net/");

        var configuration = configurationBuilder.Build();

        DatabaseConnectionString = SubsystemTestFixture.BuiDbConnectionString(
            GetConfigurationValue<string>(configuration, "mssql-data-url"),
            GetConfigurationValue<string>(configuration, "mssql-edi-database-name"));

        _serviceBusClient = new ServiceBusClient(
            GetConfigurationValue<string>(configuration, "sb-domain-relay-manage-connection-string"),
            new ServiceBusClientOptions()
            {
                TransportType =
                    ServiceBusTransportType
                        .AmqpWebSockets, // Firewall is not open for AMQP and Therefore, needs to go over WebSockets.
            });

        IntegrationEventPublisher = new IntegrationEventPublisher(
            _serviceBusClient,
            GetConfigurationValue<string>(configuration, "sbt-shres-integrationevent-received-name"),
            DatabaseConnectionString);

        LoadTestCalculationId = GetConfigurationValue<Guid>(
            configuration,
            "LOAD_TEST_CALCULATION_ID");

        EdiInboxClient = new EdiInboxClient(
            _serviceBusClient,
            GetConfigurationValue<string>(configuration, "sbq-edi-inbox-messagequeue-name"));

        // AzureWebJobsStorage connection string name/value is set implicitly from terraform as an application setting in Azure,
        // and added to the keyvault as "func-edi-api-web-jobs-storage-connection-string"
        _durableTaskManager = new DurableTaskManager(
            "AzureWebJobsStorage",
            GetConfigurationValue<string>(
                configuration,
                "func-edi-api-web-jobs-storage-connection-string"));
    }

    internal EdiInboxClient EdiInboxClient { get; }

    internal Guid LoadTestCalculationId { get; }

    internal IntegrationEventPublisher IntegrationEventPublisher { get; }

    internal string DatabaseConnectionString { get; }

    [NotNull]
    internal IDurableClient? DurableClient { get; private set; }

    public Task InitializeAsync()
    {
        DurableClient = _durableTaskManager.CreateClient("Edi01"); // Must be the same task hub name as used in B2BApi
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync().AsTask();
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusClient.DisposeAsync();
        await _durableTaskManager.DisposeAsync();

        await EdiInboxClient.DisposeAsync();
        await IntegrationEventPublisher.DisposeAsync();
    }

    private TValue GetConfigurationValue<TValue>(IConfigurationRoot root, string key)
    {
        var value = root.GetValue<TValue>(key);

        // GetValue<T> return default(T) for complex types like Guid's, so we need to compare with the default(TValue) as well.
        if (value is null || value.Equals(default(TValue)))
            throw new InvalidOperationException($"{key} was not found in configuration");

        return value;
    }
}
