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

using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

// ReSharper disable once ClassNeverInstantiated.Global -- Instantiated by XUnit
public class AcceptanceTestFixture : IAsyncLifetime
{
    internal const string ActorNumber = "5790000610976"; // Corresponds to the "Mosaic 03" actor in the UI.
    internal const string ActorGridArea = "543";
    internal const string ActorRole = "metereddataresponsible";
    private const EicFunction ActorEicFunction = EicFunction.MeteredDataResponsible;

    private readonly string _ebixCertificateThumbprint;
    private readonly Uri _azureEntraB2CTenantUrl;
    private readonly string _azureEntraFrontendAppId;
    private readonly string _azureEntraBackendBffScope;
    private readonly string _b2cUsername;
    private readonly string _b2cPassword;

    public AcceptanceTestFixture()
    {
        var root = new ConfigurationBuilder()
            .AddJsonFile("integrationtest.local.settings.json", true)
            .AddEnvironmentVariables()
            .Build();
        var secretsConfiguration = BuildSecretsConfiguration(root);

        var sqlServer = secretsConfiguration.GetValue<string>("mssql-data-url") ?? throw new InvalidOperationException("mssql-data-url secret is not set in configuration");
        var databaseName = secretsConfiguration.GetValue<string>("mssql-edi-database-name") ?? throw new InvalidOperationException("mssql-edi-database-name secret is not set in configuration");
        var dbConnectionString = $"Server={sqlServer};Authentication=Active Directory Default;Database={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        ConnectionString = dbConnectionString;

        var serviceBusConnectionString = secretsConfiguration.GetValue<string>("sb-domain-relay-manage-connection-string") ?? throw new InvalidOperationException("sb-domain-relay-manage-connection-string secret is not set in configuration");
        var topicName = secretsConfiguration.GetValue<string>("sbt-shres-integrationevent-received-name") ?? throw new InvalidOperationException("sbt-shres-integrationevent-received-name secret is not set in configuration");
        MeteredDataResponsibleAzpToken = root.GetValue<string>("AZP_TOKEN") ?? throw new InvalidOperationException("AZP_TOKEN is not set in configuration");
        EnergySupplierAzpToken = root.GetValue<string>("Energy_Supplier_AZP_TOKEN") ?? throw new InvalidOperationException("AZP_TOKEN is not set in configuration");
        ApiManagementUri = new Uri(secretsConfiguration.GetValue<string>("apim-gateway-url") ?? throw new InvalidOperationException("apim-gateway-url secret is not set in configuration"));
        AzureEntraTenantId = root.GetValue<string>("AZURE_ENTRA_TENANT_ID") ?? "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        AzureEntraBackendAppId = root.GetValue<string>("AZURE_ENTRA_BACKEND_APP_ID") ?? "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        AzureEntraClientId = root.GetValue<string>("AZURE_ENTRA_CLIENT_ID") ?? "D8E67800-B7EF-4025-90BB-FE06E1639117";
        AzureEntraClientSecret = root.GetValue<string>("AZURE_ENTRA_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_ENTRA_CLIENT_SECRET is not set in configuration");
        _ebixCertificateThumbprint = root.GetValue<string>("EBIX_CERTIFICATE_THUMBPRINT") ?? "39D64F012A19C6F6FDFB0EA91D417873599D3325";
        EbixCertificatePassword = root.GetValue<string>("EBIX_CERTIFICATE_PASSWORD") ?? throw new InvalidOperationException("EBIX_CERTIFICATE_PASSWORD is not set in configuration");
        EdiB2BBaseUri = new Uri(secretsConfiguration.GetValue<string>("func-edi-api-base-url") ?? throw new InvalidOperationException("func-edi-api-base-url secret is not set in configuration"));
        _azureEntraB2CTenantUrl = new Uri(root.GetValue<string>("AZURE_B2C_TENANT_URL") ?? "https://devdatahubb2c.b2clogin.com/tfp/devdatahubb2c.onmicrosoft.com/B2C_1_ROPC_Auth/oauth2/v2.0/token");
        _azureEntraFrontendAppId = root.GetValue<string>("AZURE_ENTRA_FRONTEND_APP_ID") ?? "bf76fc24-cfec-498f-8979-ab4123792472";
        _azureEntraBackendBffScope = root.GetValue<string>("AZURE_ENTRA_BACKEND_BFF_SCOPE") ?? "https://devDataHubB2C.onmicrosoft.com/backend-bff/api";
        MarketParticipantUri = new Uri(root.GetValue<string>("MARKET_PARTICIPANT_URI") ?? "https://app-webapi-markpart-u-001.azurewebsites.net");
        B2CApiUri = new Uri(root.GetValue<string>("B2C_API_URI") ?? "https://app-b2cwebapi-edi-u-001.azurewebsites.net");
        // _b2cUsername = root.GetValue<string>("B2C_USERNAME") ?? throw new InvalidOperationException("B2C_USERNAME is not set in configuration");
        // _b2cPassword = root.GetValue<string>("B2C_PASSWORD") ?? throw new InvalidOperationException("B2C_PASSWORD is not set in configuration");
        _b2cUsername = string.Empty;
        _b2cPassword = string.Empty;
        EventPublisher = new IntegrationEventPublisher(serviceBusConnectionString, topicName);
        B2CAuthorizedHttpClient = new AsyncLazy<HttpClient>(CreateB2CAuthorizedHttpClientAsync);
    }

    internal Uri MarketParticipantUri { get; set; }

    internal Uri B2CApiUri { get; set; }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string ConnectionString { get; }

    internal string MeteredDataResponsibleAzpToken { get; }

    internal string EnergySupplierAzpToken { get; }

    internal Uri ApiManagementUri { get; }

    internal string AzureEntraClientId { get; }

    internal string AzureEntraClientSecret { get; }

    internal string AzureEntraTenantId { get; }

    internal string AzureEntraBackendAppId { get; }

    internal string EbixCertificatePassword { get; }

    internal Uri EdiB2BBaseUri { get; }

    internal AsyncLazy<HttpClient> B2CAuthorizedHttpClient { get; }

    public async Task InitializeAsync()
    {
        var actorActivated = ActorFactory.CreateActorActivated(ActorNumber, MeteredDataResponsibleAzpToken);
        var actorCertificateAssigned = ActorCertificateFactory.CreateActorCertificateAssigned(ActorNumber, ActorEicFunction, _ebixCertificateThumbprint);
        var gridAreaOwnerAssigned = GridAreaFactory.AssignedGridAreaOwner(ActorNumber, ActorGridArea, ActorEicFunction);

        var initializeTasks = new List<Task>
        {
            EventPublisher.PublishAsync(ActorActivated.EventName, actorActivated.ToByteArray()),
            EventPublisher.PublishAsync(ActorCertificateCredentialsAssigned.EventName, actorCertificateAssigned.ToByteArray()),
            EventPublisher.PublishAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnerAssigned.ToByteArray()),
        };

        await Task.WhenAll(initializeTasks).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await EventPublisher.DisposeAsync().ConfigureAwait(false);

        if (B2CAuthorizedHttpClient.IsStarted)
            (await B2CAuthorizedHttpClient).Dispose();
    }

    private static IConfigurationRoot BuildSecretsConfiguration(IConfigurationRoot root)
    {
        var sharedKeyVaultName = root.GetValue<string>("SHARED_KEYVAULT_NAME");
        var sharedKeyVaultUrl = $"https://{sharedKeyVaultName}.vault.azure.net/";

        return new ConfigurationBuilder()
            .AddAuthenticatedAzureKeyVault(sharedKeyVaultUrl)
            .Build();
    }

    private Task<HttpClient> CreateB2CAuthorizedHttpClientAsync()
    {
        var httpClient = new HttpClient();
        return Task.FromResult(httpClient);
        // var tokenRetriever = new B2CTokenRetriever(httpClient, _azureEntraB2CTenantUrl, _azureEntraBackendBffScope, _azureEntraFrontendAppId, MarketParticipantUri);
        // var token = await tokenRetriever.GetB2CTokenAsync(_b2cUsername, _b2cPassword).ConfigureAwait(false);
        //
        // httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        //
        //return httpClient;
    }
}
