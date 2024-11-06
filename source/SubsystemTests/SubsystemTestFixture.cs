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
using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.B2C;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.Ebix;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;
using Xunit.Abstractions;
// DO NOT REMOVE THIS! use in debug mode

namespace Energinet.DataHub.EDI.SubsystemTests;

// ReSharper disable once ClassNeverInstantiated.Global -- Instantiated by XUnit
public class SubsystemTestFixture : IAsyncLifetime
{
    internal const string EbixActorGridArea = "543";
    internal const string CimActorGridArea = "804";

    internal const string ActorNumber = "5790000610976"; // Corresponds to the "Mosaic 03" actor in the UI.
    internal const string ActorRole = "metereddataresponsible";

    internal const string
        EdiSubsystemTestCimEnergySupplierNumber =
            "5790000392551"; // Corresponds to the "EDI - SUBSYSTEM TEST CIM" in the UI. Same as B2BEnergySupplierAuthorizedHttpClient

    internal const string
        EZTestCimActorNumber =
            "5790001330552"; // Corresponds to the "EDI - SUBSYSTEM TEST SYSTEM OPERATØR". Same as B2BSystemOperatorAuthorizedHttpClient

    internal const string
        ChargeOwnerId =
            "5790000391919"; // For now is a dummy value, but when we support multiple receivers, this will be the charge owners GLN.

    internal const string
        B2CActorNumber =
            "5790001330583"; // Corresponds to the "Energinet DataHub A/S (DataHub systemadministrator)" actor in the UI.

    private readonly Uri _azureEntraB2CTenantUrl;
    private readonly string _azureEntraFrontendAppId;
    private readonly string _azureEntraBackendBffScope;

    private readonly string _azureB2CTenantId;
    private readonly string _azureEntraBackendAppId;

    public SubsystemTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("subsystemtests.dev002.settings.json", true)
            .AddEnvironmentVariables();

        var jsonConfiguration = configurationBuilder.Build();
        var sharedKeyVaultName = GetConfigurationValue<string>(jsonConfiguration, "SHARED_KEYVAULT_NAME");
        var ediKeyVaultName = GetConfigurationValue<string>(jsonConfiguration, "INTERNAL_KEYVAULT_NAME");

        configurationBuilder = configurationBuilder
            .AddAuthenticatedAzureKeyVault($"https://{sharedKeyVaultName}.vault.azure.net/")
            .AddAuthenticatedAzureKeyVault($"https://{ediKeyVaultName}.vault.azure.net/");

        var root = configurationBuilder.Build();

        var sqlServer = GetConfigurationValue<string>(root, "mssql-data-url");
        var databaseName = GetConfigurationValue<string>(root, "mssql-edi-database-name");

        var dbConnectionString = BuildDbConnectionString(sqlServer, databaseName);
        ConnectionString = dbConnectionString;

        var serviceBusFullyQualifiedNamespace = $"{GetConfigurationValue<string>(root, "sb-domain-relay-namespace-name")}.servicebus.windows.net";
        var topicName = GetConfigurationValue<string>(root, "sbt-shres-integrationevent-received-name");
        var ediInboxQueueName = GetConfigurationValue<string>(root, "sbq-edi-inbox-messagequeue-name");

        _azureB2CTenantId = GetConfigurationValue<string>(root, "b2c-tenant-id", defaultValue: "e9aa9b15-7200-441e-b255-927506b3494");
        _azureEntraBackendAppId = GetConfigurationValue<string>(root, "backend-b2b-app-id");

        ApiManagementUri = new Uri(GetConfigurationValue<string>(root, "apim-gateway-url"));
        EbixUri = new Uri(
            baseUri: new Uri(GetConfigurationValue<string>(root, "EBIX_APIM_URL")),
            relativeUri: "/ebix");

        B2BClients = new B2BClients(
            MeteredDataResponsible: CreateLazyB2BHttpClient(new B2BCredentials(
                GetConfigurationValue<string>(root, "METERED_DATA_RESPONSIBLE_CLIENT_ID"),
                GetConfigurationValue<string>(root, "METERED_DATA_RESPONSIBLE_CLIENT_SECRET"))),
            EnergySupplier: CreateLazyB2BHttpClient(new B2BCredentials(
                    GetConfigurationValue<string>(root, "ENERGY_SUPPLIER_CLIENT_ID"),
                    GetConfigurationValue<string>(root, "ENERGY_SUPPLIER_CLIENT_SECRET"))),
            SystemOperator: CreateLazyB2BHttpClient(new B2BCredentials(
                GetConfigurationValue<string>(root, "SYSTEM_OPERATOR_CLIENT_ID"),
                GetConfigurationValue<string>(root, "SYSTEM_OPERATOR_CLIENT_SECRET"))));

        BalanceFixingCalculationId = GetConfigurationValue<Guid>(root, "BALANCE_FIXING_CALCULATION_ID");
        WholesaleFixingCalculationId = GetConfigurationValue<Guid>(root, "WHOLESALE_FIXING_CALCULATION_ID");

        EbixMeteredDataResponsibleCertificateThumbprint = GetConfigurationValue<string>(
            root,
            "EBIX_CERTIFICATE_THUMBPRINT",
            defaultValue: "39D64F012A19C6F6FDFB0EA91D417873599D3325");

        // The actor 5790000610976 as Metered Data Responsible
        EbixMeteredDataResponsibleCredentials = new EbixCredentials(
            certificateName: "DH3-test-mosaik-1-private-and-public.pfx",
            certificatePassword: GetConfigurationValue<string>(root, "EBIX_CERTIFICATE_PASSWORD_MDR"));

        // The actor 0266518730406 as Grid Access Provider (owns area 757)
        EbixGridAccessProviderCredentials = new EbixCredentials(
            certificateName: "DH3-test-Mosaic-GridAccessProvider.pfx",
            certificatePassword: GetConfigurationValue<string>(root, "EBIX_CERTIFICATE_PASSWORD_DDM"));

        // The actor 5790000610976 as Energy Supplier
        EbixEnergySupplierCredentials = new EbixCredentials(
            certificateName: "DH3-test-mosaik-energysupplier-private-and-public.pfx",
            certificatePassword: GetConfigurationValue<string>(root, "EBIX_CERTIFICATE_PASSWORD_ES"));

        _azureEntraB2CTenantUrl = new Uri(
            GetConfigurationValue<string>(
                root,
                "AZURE_B2C_TENANT_URL",
                defaultValue: "https://devdatahubb2c.b2clogin.com/tfp/devdatahubb2c.onmicrosoft.com/B2C_1_ROPC_Auth/oauth2/v2.0/token"));

        _azureEntraFrontendAppId = GetConfigurationValue<string>(
                                       root,
                                       "AZURE_ENTRA_FRONTEND_APP_ID",
                                       defaultValue: "bf76fc24-cfec-498f-8979-ab4123792472");

        _azureEntraBackendBffScope = GetConfigurationValue<string>(
                                         root,
                                         "AZURE_ENTRA_BACKEND_BFF_SCOPE",
                                         defaultValue: "https://devDataHubB2C.onmicrosoft.com/backend-bff/api");

        MarketParticipantUri = new Uri(GetConfigurationValue<string>(
            root,
            "MARKET_PARTICIPANT_URI",
            defaultValue: "https://app-webapi-markpart-u-001.azurewebsites.net"));

        EdiB2CWebApiUri = new Uri(GetConfigurationValue<string>(root, "EDI_B2C_WEB_API_URI"));

        var b2cUsername = GetConfigurationValue<string>(root, "B2C_USERNAME");
        var b2cPassword = GetConfigurationValue<string>(root, "B2C_PASSWORD");

        B2CClients = new B2CClients(
            DatahubAdministrator: CreateLazyB2CHttpClient(new B2CCredentials(
                b2cUsername,
                b2cPassword,
                GetConfigurationValue<Guid>(root, "B2C_ADMINISTRATOR_ACTOR_ID"))),
            EnergySupplier: CreateLazyB2CHttpClient(new B2CCredentials(
                b2cUsername,
                b2cPassword,
                GetConfigurationValue<Guid>(root, "B2C_ENERGY_SUPPLIER_ACTOR_ID"))));

        var credential = new DefaultAzureCredential();
        ServiceBusClient = new ServiceBusClient(serviceBusFullyQualifiedNamespace, credential);
        EventPublisher = new IntegrationEventPublisher(ServiceBusClient, topicName, dbConnectionString);
        EdiInboxClient = new EdiInboxClient(ServiceBusClient, ediInboxQueueName);

        DurableTaskManager = new DurableTaskManager(
            "OrchestrationsStorageConnectionString",
            GetConfigurationValue<string>(root, "func-edi-api-taskhub-storage-connection-string"));
    }

    internal B2BClients B2BClients { get; }

    internal B2CClients B2CClients { get; }

    internal EbixCredentials EbixEnergySupplierCredentials { get; }

    internal EbixCredentials EbixMeteredDataResponsibleCredentials { get; }

    internal EbixCredentials EbixGridAccessProviderCredentials { get; }

    internal string EbixMeteredDataResponsibleCertificateThumbprint { get; }

    internal Uri EdiB2CWebApiUri { get; }

    internal Guid BalanceFixingCalculationId { get; }

    internal Guid WholesaleFixingCalculationId { get; }

    internal ITestOutputHelper? Logger { get; set; }

    internal DurableTaskManager DurableTaskManager { get; }

    [NotNull]
    internal IDurableClient? DurableClient { get; set; }

    internal Uri MarketParticipantUri { get; }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal EdiInboxClient EdiInboxClient { get; }

    internal string ConnectionString { get; }

    internal Uri ApiManagementUri { get; }

    internal Uri EbixUri { get; }

    private ServiceBusClient ServiceBusClient { get; }

    public Task InitializeAsync()
    {
        DurableClient = DurableTaskManager.CreateClient("Edi01"); // Must be the same task hub name as used in B2BApi

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Close subclients before client (ServiceBusClient)
        await EventPublisher.DisposeAsync();
        await EdiInboxClient.DisposeAsync();
        await ServiceBusClient.DisposeAsync();

        await B2BClients.DisposeAsync();
        await B2CClients.DisposeAsync();
    }

    internal static string BuildDbConnectionString(string sqlServer, string databaseName)
    {
        return $"Server={sqlServer};Authentication=Active Directory Default;Database={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
    }

    private TValue GetConfigurationValue<TValue>(IConfigurationRoot root, string key)
    {
        var value = root.GetValue<TValue>(key);

        // GetValue<T> return default(T) for complex types like Guid's, so we need to compare with the default(TValue) as well.
        if (value is null || value.Equals(default(TValue)))
            throw new InvalidOperationException($"{key} was not found in configuration");

        return value;
    }

    private TValue GetConfigurationValue<TValue>(IConfigurationRoot root, string key, TValue defaultValue)
    {
        var value = root.GetValue<TValue>(key);

        // GetValue<T> return default(T) for complex types like Guid's, so we need to compare with the default(TValue) as well.
        if (value is null || value.Equals(default(TValue)))
            return defaultValue;

        return value;
    }

    private AsyncLazy<HttpClient> CreateLazyB2BHttpClient(B2BCredentials credentials)
    {
        return new AsyncLazy<HttpClient>(async () =>
        {
            var httpTokenClient = new HttpClient();

            var tokenRetriever = new B2BTokenReceiver(
                httpTokenClient,
                _azureB2CTenantId,
                _azureEntraBackendAppId,
                GetLogger());
            var token = await tokenRetriever
                .GetB2BTokenAsync(credentials)
                .ConfigureAwait(false);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            httpClient.BaseAddress = ApiManagementUri;

            return httpClient;
        });
    }

    private AsyncLazy<HttpClient> CreateLazyB2CHttpClient(B2CCredentials credentials)
    {
        return new AsyncLazy<HttpClient>(
            async () =>
            {
                var httpClient = new HttpClient();

                var tokenRetriever = new B2CTokenRetriever(
                    httpClient,
                    _azureEntraB2CTenantUrl,
                    _azureEntraBackendBffScope,
                    _azureEntraFrontendAppId,
                    MarketParticipantUri,
                    GetLogger());
                var token = await tokenRetriever.GetB2CTokenAsync(credentials).ConfigureAwait(false);

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                return httpClient;
            });
    }

    private ITestOutputHelper GetLogger()
    {
        return Logger
               ?? throw new NullReferenceException(
                   "SubsystemTestFixture.Logger must be set from tests. Inject ITestOutputHelper in tests constructor and set it on the fixture");
    }
}

public record B2BClients(
    AsyncLazy<HttpClient> MeteredDataResponsible,
    AsyncLazy<HttpClient> EnergySupplier,
    AsyncLazy<HttpClient> SystemOperator) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        if (MeteredDataResponsible is { IsStarted: true, Task.IsFaulted: false })
            (await MeteredDataResponsible).Dispose();
        if (EnergySupplier is { IsStarted: true, Task.IsFaulted: false })
            (await EnergySupplier).Dispose();
        if (SystemOperator is { IsStarted: true, Task.IsFaulted: false })
            (await SystemOperator).Dispose();
    }
}

public record B2CClients(
    AsyncLazy<HttpClient> DatahubAdministrator,
    AsyncLazy<HttpClient> EnergySupplier) : IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        if (DatahubAdministrator is { IsStarted: true, Task.IsFaulted: false })
            (await DatahubAdministrator).Dispose();
        if (EnergySupplier is { IsStarted: true, Task.IsFaulted: false })
            (await EnergySupplier).Dispose();
    }
}
