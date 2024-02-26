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

using System.Net.Http.Headers;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration; // DO NOT REMOVE THIS! use in debug mode
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace Energinet.DataHub.EDI.AcceptanceTests;

// ReSharper disable once ClassNeverInstantiated.Global -- Instantiated by XUnit
public class AcceptanceTestFixture : IAsyncLifetime
{
    internal const string EbixActorGridArea = "543";
    internal const string CimActorGridArea = "804";

    internal const string ActorNumber = "5790000610976"; // Corresponds to the "Mosaic 03" actor in the UI.
    internal const string ActorRole = "metereddataresponsible";

    internal const string EdiSubsystemTestCimActorNumber = "5790000392551"; // Corresponds to the "EDI - SUBSYSTEM TEST CIM" in the UI.
    internal const string ChargeOwnerId = "5790000391919"; // For now is a dummy value, but when we support multiple receivers, this will be the receiver's real GSRN.

    private readonly Uri _azureEntraB2CTenantUrl;
    private readonly string _azureEntraFrontendAppId;
    private readonly string _azureEntraBackendBffScope;
    private readonly string _b2cUsername;
    private readonly string _b2cPassword;

    public AcceptanceTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("acceptancetest.local.settings.json", true)
            .AddEnvironmentVariables();

        var jsonConfiguration = configurationBuilder.Build();
        var keyVaultName = jsonConfiguration.GetValue<string>("SHARED_KEYVAULT_NAME");

        configurationBuilder = configurationBuilder.AddAuthenticatedAzureKeyVault($"https://{keyVaultName}.vault.azure.net/");

        var root = configurationBuilder.Build();

        var sqlServer = root.GetValue<string>("mssql-data-url") ?? throw new InvalidOperationException("mssql-data-url secret is not set in configuration");
        var databaseName = root.GetValue<string>("mssql-edi-database-name") ?? throw new InvalidOperationException("mssql-edi-database-name secret is not set in configuration");
        var dbConnectionString = $"Server={sqlServer};Authentication=Active Directory Default;Database={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        ConnectionString = dbConnectionString;

        var serviceBusConnectionString = root.GetValue<string>("sb-domain-relay-manage-connection-string") ?? throw new InvalidOperationException("sb-domain-relay-manage-connection-string secret is not set in configuration");
        var topicName = root.GetValue<string>("sbt-shres-integrationevent-received-name") ?? throw new InvalidOperationException("sbt-shres-integrationevent-received-name secret is not set in configuration");

        var azureB2CTenantId = root.GetValue<string>("b2c-tenant-id") ?? "e9aa9b15-7200-441e-b255-927506b3494";
        var azureEntraBackendAppId = root.GetValue<string>("backend-b2b-app-id") ?? throw new InvalidOperationException("backend-b2b-app-id is not set in configuration");
        ApiManagementUri = new Uri(root.GetValue<string>("apim-gateway-url") ?? throw new InvalidOperationException("apim-gateway-url secret is not set in configuration"));

        var meteredDataResponsibleId = root.GetValue<string>("METERED_DATA_RESPONSIBLE_CLIENT_ID") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_CLIENT_ID is not set in configuration");
        var meteredDataResponsibleSecret = root.GetValue<string>("METERED_DATA_RESPONSIBLE_CLIENT_SECRET") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_CLIENT_SECRET is not set in configuration");
        B2BMeteredDataResponsibleAuthorizedHttpClient = new AsyncLazy<HttpClient>(() => CreateB2BMeteredDataResponsibleAuthorizedHttpClientAsync(azureB2CTenantId, azureEntraBackendAppId, meteredDataResponsibleId, meteredDataResponsibleSecret, ApiManagementUri));

        var energySupplierId = root.GetValue<string>("ENERGY_SUPPLIER_CLIENT_ID") ?? throw new InvalidOperationException("ENERGY_SUPPLIER_CLIENT_ID is not set in configuration");
        var energySupplierSecret = root.GetValue<string>("ENERGY_SUPPLIER_CLIENT_SECRET") ?? throw new InvalidOperationException("ENERGY_SUPPLIER_CLIENT_SECRET is not set in configuration");
        B2BEnergySupplierAuthorizedHttpClient = new AsyncLazy<HttpClient>(() => CreateB2BEnergySupplierAuthorizedHttpClientAsync(azureB2CTenantId, azureEntraBackendAppId, energySupplierId, energySupplierSecret, ApiManagementUri));

        EbixCertificateThumbprint = root.GetValue<string>("EBIX_CERTIFICATE_THUMBPRINT") ?? "39D64F012A19C6F6FDFB0EA91D417873599D3325";
        EbixCertificatePassword = root.GetValue<string>("EBIX_CERTIFICATE_PASSWORD") ?? throw new InvalidOperationException("EBIX_CERTIFICATE_PASSWORD is not set in configuration");
        _azureEntraB2CTenantUrl = new Uri(root.GetValue<string>("AZURE_B2C_TENANT_URL") ?? "https://devdatahubb2c.b2clogin.com/tfp/devdatahubb2c.onmicrosoft.com/B2C_1_ROPC_Auth/oauth2/v2.0/token");
        _azureEntraFrontendAppId = root.GetValue<string>("AZURE_ENTRA_FRONTEND_APP_ID") ?? "bf76fc24-cfec-498f-8979-ab4123792472";
        _azureEntraBackendBffScope = root.GetValue<string>("AZURE_ENTRA_BACKEND_BFF_SCOPE") ?? "https://devDataHubB2C.onmicrosoft.com/backend-bff/api";
        MarketParticipantUri = new Uri(root.GetValue<string>("MARKET_PARTICIPANT_URI") ?? "https://app-webapi-markpart-u-001.azurewebsites.net");
        _b2cUsername = root.GetValue<string>("B2C_USERNAME") ?? throw new InvalidOperationException("B2C_USERNAME is not set in configuration");
        _b2cPassword = root.GetValue<string>("B2C_PASSWORD") ?? throw new InvalidOperationException("B2C_PASSWORD is not set in configuration");
        EventPublisher = new IntegrationEventPublisher(serviceBusConnectionString, topicName);
        B2CAuthorizedHttpClient = new AsyncLazy<HttpClient>(CreateB2CAuthorizedHttpClientAsync);
    }

    internal Uri MarketParticipantUri { get; set; }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string ConnectionString { get; }

    internal Uri ApiManagementUri { get; }

    internal string EbixCertificatePassword { get; }

    internal string EbixCertificateThumbprint { get; }

    internal AsyncLazy<HttpClient> B2CAuthorizedHttpClient { get; }

    internal AsyncLazy<HttpClient> B2BMeteredDataResponsibleAuthorizedHttpClient { get; }

    internal AsyncLazy<HttpClient> B2BEnergySupplierAuthorizedHttpClient { get; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await EventPublisher.DisposeAsync().ConfigureAwait(false);

        if (B2CAuthorizedHttpClient.IsStarted)
            (await B2CAuthorizedHttpClient).Dispose();
    }

    private static async Task<HttpClient> CreateB2BMeteredDataResponsibleAuthorizedHttpClientAsync(
        string azureB2CTenantId,
        string azureEntraBackendAppId,
        string clientId,
        string clientSecret,
        Uri baseAddress)
    {
        var httpTokenClient = new HttpClient();

        var tokenRetriever = new B2BTokenReceiver(httpTokenClient, azureB2CTenantId, azureEntraBackendAppId);
        var token = await tokenRetriever
            .GetB2BTokenAsync(clientId, clientSecret)
            .ConfigureAwait(false);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        httpClient.BaseAddress = baseAddress;
        return httpClient;
    }

    private static async Task<HttpClient> CreateB2BEnergySupplierAuthorizedHttpClientAsync(
        string azureB2CTenantId,
        string azureEntraBackendAppId,
        string clientId,
        string clientSecret,
        Uri baseAddress)
    {
        var httpTokenClient = new HttpClient();

        var tokenRetriever = new B2BTokenReceiver(httpTokenClient, azureB2CTenantId, azureEntraBackendAppId);
        var token = await tokenRetriever
            .GetB2BTokenAsync(clientId, clientSecret)
            .ConfigureAwait(false);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        httpClient.BaseAddress = baseAddress;
        return httpClient;
    }

    private async Task<HttpClient> CreateB2CAuthorizedHttpClientAsync()
    {
        var httpClient = new HttpClient();

        var tokenRetriever = new B2CTokenRetriever(httpClient, _azureEntraB2CTenantUrl, _azureEntraBackendBffScope, _azureEntraFrontendAppId, MarketParticipantUri);
        var token = await tokenRetriever.GetB2CTokenAsync(_b2cUsername, _b2cPassword).ConfigureAwait(false);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        return httpClient;
    }
}
