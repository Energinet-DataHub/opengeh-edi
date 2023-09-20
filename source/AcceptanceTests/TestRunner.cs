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

using AcceptanceTest.Drivers;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AcceptanceTest;

public class TestRunner : IAsyncDisposable
{
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
        ActorFactory.InsertActor(dbConnectionString, AzpToken);
    }

    internal IntegrationEventPublisher EventPublisher { get; }

    internal string AzpToken { get; }

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
