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

// ReSharper disable once ClassNeverInstantiated.Global -- Used by Dependency Injection in our tests
public class TestRunner : IAsyncDisposable
{
    internal const string AcceptanceTestCollection = "Acceptance test collection";

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

        var sqlServer = secretsConfiguration.GetValue<string>("mssql-data-url") ?? throw new InvalidOperationException("mssql-data-url secret is not set in configuration");
        var databaseName = secretsConfiguration.GetValue<string>("mssql-edi-database-name") ?? throw new InvalidOperationException("mssql-edi-database-name secret is not set in configuration");
        var dbConnectionString = $"Server={sqlServer};Authentication=Active Directory Default;Database={databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        ConnectionString = dbConnectionString;

        var serviceBusConnectionString = secretsConfiguration.GetValue<string>("sb-domain-relay-manage-connection-string") ?? throw new InvalidOperationException("sb-domain-relay-manage-connection-string secret is not set in configuration");
        var topicName = secretsConfiguration.GetValue<string>("sbt-shres-integrationevent-received-name") ?? throw new InvalidOperationException("sbt-shres-integrationevent-received-name secret is not set in configuration");
        AzpToken = root.GetValue<string>("AZP_TOKEN") ?? throw new InvalidOperationException("AZP_TOKEN is not set in configuration");
        ApiManagementUri = new Uri(secretsConfiguration.GetValue<string>("apim-gateway-url") ?? throw new InvalidOperationException("apim-gateway-url secret is not set in configuration"));
        AzureEntraTenantId = root.GetValue<string>("AZURE_ENTRA_TENANT_ID") ?? "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        AzureEntraBackendAppId = root.GetValue<string>("AZURE_ENTRA_BACKEND_APP_ID") ?? "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        AzureEntraClientId = root.GetValue<string>("AZURE_ENTRA_CLIENT_ID") ?? "D8E67800-B7EF-4025-90BB-FE06E1639117";
        AzureEntraClientSecret = root.GetValue<string>("AZURE_ENTRA_CLIENT_SECRET") ?? throw new InvalidOperationException("AZURE_ENTRA_CLIENT_SECRET is not set in configuration");
        EbixCertificateThumbprint = root.GetValue<string>("EBIX_CERTIFICATE_THUMBPRINT") ?? "39D64F012A19C6F6FDFB0EA91D417873599D3325";
        EbixCertificatePassword = root.GetValue<string>("EBIX_CERTIFICATE_PASSWORD") ?? throw new InvalidOperationException("EBIX_CERTIFICATE_PASSWORD is not set in configuration");
        EdiB2BBaseUri = new Uri(secretsConfiguration.GetValue<string>("func-edi-api-base-url") ?? throw new InvalidOperationException("func-edi-api-base-url secret is not set in configuration"));

        EventPublisher = new IntegrationEventPublisher(serviceBusConnectionString, topicName);

        var actorActivated = ActorFactory.CreateActorActivated(ActorNumber, AzpToken);
        _ = EventPublisher.PublishAsync(ActorActivated.EventName, actorActivated.ToByteArray());

        var actorCertificateAssigned = ActorCertificateFactory.CreateActorCertificateAssigned(ActorNumber, ActorEicFunction, EbixCertificateThumbprint);
        _ = EventPublisher.PublishAsync(ActorCertificateCredentialsAssigned.EventName, actorCertificateAssigned.ToByteArray());

        var gridAreaOwnerAssigned = GridAreaFactory.AssignedGridAreaOwner(ActorNumber, ActorGridArea, ActorEicFunction);
        _ = EventPublisher.PublishAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnerAssigned.ToByteArray());
    }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string ConnectionString { get; }

    internal string AzpToken { get; }

    internal Uri ApiManagementUri { get; }

    internal string AzureEntraClientId { get; }

    internal string AzureEntraClientSecret { get; }

    internal string AzureEntraTenantId { get; }

    internal string AzureEntraBackendAppId { get; }

    internal string EbixCertificatePassword { get; }

    internal Uri EdiB2BBaseUri { get; }

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
