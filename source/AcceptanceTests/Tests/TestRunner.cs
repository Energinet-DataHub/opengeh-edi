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

namespace Energinet.DataHub.EDI.AcceptanceTests.Tests;

public class TestRunner : IAsyncDisposable
{
    internal const string ActorNumber = "5790000610976"; // Corresponds to the "Mosaic 03" actor in the UI.
    internal const string ActorGridArea = "543";
    internal const string ActorRole = "metereddataresponsible";
    private const EicFunction ActorEicFunction = EicFunction.MeteredDataResponsible;

    public TestRunner()
    {
        var root = new ConfigurationBuilder()
            .AddJsonFile("integrationtest.local.settings.json", true)
            .AddEnvironmentVariables()
            .Build();
        var secretsConfiguration = BuildSecretsConfiguration(root);

        var sqlServer = secretsConfiguration.GetValue<string>("mssql-data-url")!;
        var dbConnectionString = $"Server={sqlServer};Authentication=Active Directory Default;Database=mssqldb-edi-edi-u-001;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        ConnectionString = dbConnectionString;

        var serviceBusConnectionString = secretsConfiguration.GetValue<string>("sb-domain-relay-manage-connection-string")!;
        var topicName = secretsConfiguration.GetValue<string>("sbt-shres-integrationevent-received-name")!;
        AzpToken = root.GetValue<string>("AZP_TOKEN") ?? throw new InvalidOperationException("AZP_TOKEN is not set in configuration");
        ApiManagementUri = new Uri(root.GetValue<string>("API_MANAGEMENT_URL") ?? "https://apim-shared-sharedres-u-001.azure-api.net/");
        AzureEntraTenantId = root.GetValue<string>("AZURE_ENTRA_TENANT_ID") ?? "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        AzureEntraBackendAppId = root.GetValue<string>("AZURE_ENTRA_BACKEND_APP_ID") ?? "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        AzureEntraClientId = root.GetValue<string>("AZURE_ENTRA_CLIENT_ID") ?? "D8E67800-B7EF-4025-90BB-FE06E1639117";
        AzureEntraClientSecret = root.GetValue<string>("AZURE_ENTRA_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_ENTRA_CLIENT_SECRET is not set in configuration");
        EbixCertificateThumbprint = root.GetValue<string>("EBIX_CERTIFICATE_THUMBPRINT") ?? "39D64F012A19C6F6FDFB0EA91D417873599D3325";
        EbixCertificatePassword = root.GetValue<string>("EBIX_CERTIFICATE_PASSWORD") ?? throw new InvalidOperationException("EBIX_CERTIFICATE_PASSWORD is not set in configuration");
        AzureEntraB2CTenantUrl = new Uri(root.GetValue<string>("AZURE_B2C_TENANT_URL") ?? "https://devdatahubb2c.b2clogin.com/tfp/devdatahubb2c.onmicrosoft.com/B2C_1_ROPC_Auth/oauth2/v2.0/token");
        AzureEntraFrontendAppId = root.GetValue<string>("AZURE_ENTRA_FRONTEND_APP_ID") ?? "bf76fc24-cfec-498f-8979-ab4123792472";
        AzureEntraBackendBffScope = root.GetValue<string>("AZURE_ENTRA_BACKEND_BFF_SCOPE") ?? "https://devDataHubB2C.onmicrosoft.com/backend-bff/api";
        B2cUsername = root.GetValue<string>("B2C_USERNAME") ?? throw new InvalidOperationException("B2C_USERNAME is not set in configuration");
        B2cPassword = root.GetValue<string>("B2C_PASSWORD") ?? throw new InvalidOperationException("B2C_PASSWORD is not set in configuration");

        EventPublisher = new IntegrationEventPublisher(serviceBusConnectionString, topicName);

        var actorActivated = ActorFactory.CreateActorActivated(ActorNumber, AzpToken);
        _ = EventPublisher.PublishAsync(ActorActivated.EventName, actorActivated.ToByteArray());

        var actorCertificateAssigned = ActorCertificateFactory.CreateActorCertificateAssigned(ActorNumber, ActorEicFunction, EbixCertificateThumbprint);
        _ = EventPublisher.PublishAsync(ActorCertificateCredentialsAssigned.EventName, actorCertificateAssigned.ToByteArray());

        var gridAreaOwnerAssigned = GridAreaFactory.AssignedGridAreaOwner(ActorNumber, ActorGridArea, ActorEicFunction);
        _ = EventPublisher.PublishAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnerAssigned.ToByteArray());
    }

    public Uri AzureEntraB2CTenantUrl { get; }

    public string AzureEntraFrontendAppId { get; }

    public string AzureEntraBackendBffScope { get; }

    public string B2cUsername { get; }

    public string B2cPassword { get; }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string ConnectionString { get; }

    internal string AzpToken { get; }

    internal Uri ApiManagementUri { get; }

    internal string AzureEntraClientId { get; }

    internal string AzureEntraClientSecret { get; }

    internal string AzureEntraTenantId { get; }

    internal string AzureEntraBackendAppId { get; }

    internal string EbixCertificatePassword { get; }

    private string EbixCertificateThumbprint { get; }

    public async ValueTask DisposeAsync()
    {
        await EventPublisher.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private static IConfigurationRoot BuildSecretsConfiguration(IConfigurationRoot root)
    {
        var sharedKeyVaultName = root.GetValue<string>("SHARED_KEYVAULT_NAME");
        var sharedKeyVaultUrl = $"https://{sharedKeyVaultName}.vault.azure.net/";

        return new ConfigurationBuilder()
            .AddAuthenticatedAzureKeyVault(sharedKeyVaultUrl)
            .Build();
    }
}
