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
using Microsoft.Extensions.Configuration;

namespace Energinet.DataHub.EDI.AcceptanceTests;

public class TestRunner : IAsyncDisposable
{
    internal const string BalanceResponsibleActorNumber = "5790000392551"; // Svarer til "Test til Phoenix" aktøren i UI'en. Aktørnummer i UI og her stemmer ikke overens (måske vores database har dårlig data?).
    internal const string BalanceResponsibleActorRole = "balanceresponsibleparty";

    protected TestRunner()
    {
        var root = new ConfigurationBuilder()
            .AddJsonFile("integrationtest.local.settings.json", true)
            .AddEnvironmentVariables()
            .Build();
        var secretsConfiguration = BuildSecretsConfiguration(root);

        var connectionString = secretsConfiguration.GetValue<string>("sb-domain-relay-manage-connection-string")!;
        var topicName = secretsConfiguration.GetValue<string>("sbt-shres-integrationevent-received-name")!;
        EventPublisher = new IntegrationEventPublisher(connectionString, topicName);
        AzpToken = root.GetValue<string>("AZP_TOKEN")!;

        var sqlServer = secretsConfiguration.GetValue<string>("mssql-data-url")!;
        var sqlUserName = secretsConfiguration.GetValue<string>("mssql-data-admin-user-name")!;
        var sqlUserPassword = secretsConfiguration.GetValue<string>("mssql-data-admin-user-password")!;
        var sqlDatabaseName = "mssqldb-edi-edi-u-001";

        var dbConnectionString = $"Server={sqlServer};Initial Catalog={sqlDatabaseName};User Id={sqlUserName};Password={sqlUserPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
        // ActorFactory.InsertActor(dbConnectionString, AzpToken);

        AzureEntraTenantId = "4a7411ea-ac71-4b63-9647-b8bd4c5a20e0";
        AzureEntraBackendAppId = "fe8b720c-fda4-4aaa-9c6d-c0d2ed6584fe";
        AzureEntraClientId = "D8E67800-B7EF-4025-90BB-FE06E1639117";
        AzureEntraClientSecret = root.GetValue<string>("AZURE_ENTRA_CLIENT_SECRET")!;
    }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string AzpToken { get; }

    internal string AzureEntraClientId { get; }

    internal string AzureEntraClientSecret { get; }

    internal string AzureEntraTenantId { get; }

    internal string AzureEntraBackendAppId { get; }

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
