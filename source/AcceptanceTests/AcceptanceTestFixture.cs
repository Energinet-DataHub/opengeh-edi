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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;
using Energinet.DataHub.EDI.AcceptanceTests.Factories;
using Energinet.DataHub.EDI.AcceptanceTests.TestData;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Microsoft.Extensions.Configuration;
using Nito.AsyncEx;

namespace Energinet.DataHub.EDI.AcceptanceTests;

// ReSharper disable once ClassNeverInstantiated.Global -- Instantiated by XUnit
public class AcceptanceTestFixture : IAsyncLifetime
{
    internal const string ActorNumber = "5790000610976"; // Corresponds to the "Mosaic 03" actor in the UI.
    internal const string ActorGridArea = "543";
    private const EicFunction ActorEicFunction = EicFunction.MeteredDataResponsible;

    private readonly string _ebixCertificateThumbprint;
    private readonly string _b2cUsername;
    private readonly string _b2cPassword;

    public AcceptanceTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("acceptancetest.local.settings.json", true)
            .AddEnvironmentVariables();

        #if !DEBUG
        var jsonConfiguration = configurationBuilder.Build();
        var keyVaultName = jsonConfiguration.GetValue<string>("SHARED_KEYVAULT_NAME");

        configurationBuilder = configurationBuilder.AddAuthenticatedAzureKeyVault($"https://{keyVaultName}.vault.azure.net/");
        #endif

        var root = configurationBuilder.Build();

        var sqlServer = root.GetValue<string>("mssql-data-url") ?? throw new InvalidOperationException("mssql-data-url secret is not set in configuration");
        var databaseName = root.GetValue<string>("mssql-edi-database-name") ?? throw new InvalidOperationException("mssql-edi-database-name secret is not set in configuration");
        var dbConnectionString = $"Server={sqlServer};Authentication=Active Directory Default;Database={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        ConnectionString = dbConnectionString;

        var serviceBusConnectionString = root.GetValue<string>("sb-domain-relay-manage-connection-string") ?? throw new InvalidOperationException("sb-domain-relay-manage-connection-string secret is not set in configuration");
        var topicName = root.GetValue<string>("sbt-shres-integrationevent-received-name") ?? throw new InvalidOperationException("sbt-shres-integrationevent-received-name secret is not set in configuration");

        var azureEntraClientId = root.GetValue<string>("AZURE_ENTRA_CLIENT_ID") ?? "D8E67800-B7EF-4025-90BB-FE06E1639117";
        var meteredDataResponsibleSecret = root.GetValue<string>("METERED_DATA_RESPONSIBLE_CLIENT_SECRET") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_CLIENT_SECRET is not set in configuration");
        var meteredDataResponsibleAzpToken = root.GetValue<string>("METERED_DATA_RESPONSIBLE_AZP_TOKEN") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_AZP_TOKEN is not set in configuration");
        MeteredDataResponsibleCredential = new ActorCredential(azureEntraClientId, meteredDataResponsibleSecret, meteredDataResponsibleAzpToken);

        var energySupplierSecret = root.GetValue<string>("AZURE_ENTRA_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_ENTRA_CLIENT_SECRET is not set in configuration");
        var energySupplierAzpToken = root.GetValue<string>("ENERGY_SUPPLIER_AZP_TOKEN") ?? throw new InvalidOperationException("ENERGY_SUPPLIER_AZP_TOKEN is not set in configuration");
        EnergySupplierCredential = new ActorCredential(azureEntraClientId, energySupplierSecret, energySupplierAzpToken);

        ApiManagementUri = new Uri(root.GetValue<string>("apim-gateway-url") ?? throw new InvalidOperationException("apim-gateway-url secret is not set in configuration"));
        AzureEntraTenantId = root.GetValue<string>("AZURE_ENTRA_TENANT_ID") ?? "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        AzureEntraBackendAppId = root.GetValue<string>("AZURE_ENTRA_BACKEND_APP_ID") ?? "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        _ebixCertificateThumbprint = root.GetValue<string>("EBIX_CERTIFICATE_THUMBPRINT") ?? "39D64F012A19C6F6FDFB0EA91D417873599D3325";
        EbixCertificatePassword = root.GetValue<string>("EBIX_CERTIFICATE_PASSWORD") ?? throw new InvalidOperationException("EBIX_CERTIFICATE_PASSWORD is not set in configuration");
        EdiB2BBaseUri = new Uri(root.GetValue<string>("func-edi-api-base-url") ?? throw new InvalidOperationException("func-edi-api-base-url secret is not set in configuration"));
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

    internal ActorCredential MeteredDataResponsibleCredential { get; }

    internal ActorCredential EnergySupplierCredential { get; }

    internal Uri ApiManagementUri { get; }

    internal string AzureEntraTenantId { get; }

    internal string AzureEntraBackendAppId { get; }

    internal string EbixCertificatePassword { get; }

    internal Uri EdiB2BBaseUri { get; }

    internal AsyncLazy<HttpClient> B2CAuthorizedHttpClient { get; }

    public async Task InitializeAsync()
    {
        var actorActivated = ActorFactory.CreateActorActivated(ActorNumber, MeteredDataResponsibleCredential.AzpToken);
        var actorCertificateAssigned = ActorCertificateFactory.CreateActorCertificateAssigned(ActorNumber, ActorEicFunction, _ebixCertificateThumbprint);
        var gridAreaOwnerAssigned = GridAreaFactory.AssignedGridAreaOwner(ActorNumber, ActorGridArea, ActorEicFunction);

        var initializeTasks = new List<Task>
        {
#if !DEBUG // Locally we cannot access the Azure Service Bus, so this will fail
            EventPublisher.PublishAsync(ActorActivated.EventName, actorActivated.ToByteArray()),
            EventPublisher.PublishAsync(ActorCertificateCredentialsAssigned.EventName, actorCertificateAssigned.ToByteArray()),
            EventPublisher.PublishAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnerAssigned.ToByteArray()),
#endif
        };

        await Task.WhenAll(initializeTasks).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await EventPublisher.DisposeAsync().ConfigureAwait(false);

        if (B2CAuthorizedHttpClient.IsStarted)
            (await B2CAuthorizedHttpClient).Dispose();
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
