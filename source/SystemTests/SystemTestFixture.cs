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
using Energinet.DataHub.EDI.SystemTests.Drivers;
using Energinet.DataHub.EDI.SystemTests.Dsl;
using Energinet.DataHub.EDI.SystemTests.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Energinet.DataHub.EDI.SystemTests;

public class SystemTestFixture : IAsyncLifetime
{
    public SystemTestFixture()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("systemtests.dev001.settings.json", true)
            .AddEnvironmentVariables();

        var jsonConfiguration = configurationBuilder.Build();
        var keyVaultName = jsonConfiguration.GetValue<string>("SHARED_KEYVAULT_NAME");

        configurationBuilder = configurationBuilder.AddAuthenticatedAzureKeyVault($"https://{keyVaultName}.vault.azure.net/");

        var root = configurationBuilder.Build();

        var tenantId = root.GetValue<string>("b2c-tenant-id") ?? throw new InvalidOperationException("b2c-tenant-id is not set in configuration");
        var backendAppId = root.GetValue<string>("backend-b2b-app-id") ?? throw new InvalidOperationException("backend-b2b-app-id is not set in configuration");
        var apiManagementUri = new Uri(root.GetValue<string>("apim-gateway-url") ?? throw new InvalidOperationException("apim-gateway-url secret is not set in configuration"));
        EdiDriver = new EdiDriver(apiManagementUri, tenantId, backendAppId);

        var meteredDataResponsibleClientId = root.GetValue<string>("METERED_DATA_RESPONSIBLE_CLIENT_ID") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_CLIENT_ID is not set in configuration");
        var meteredDataResponsibleClientSecret = root.GetValue<string>("METERED_DATA_RESPONSIBLE_CLIENT_SECRET") ?? throw new InvalidOperationException("METERED_DATA_RESPONSIBLE_CLIENT_SECRET is not set in configuration");
        MeteredDataResponsible = new Actor(meteredDataResponsibleClientId, meteredDataResponsibleClientSecret);

        var systemOperatorClientId = root.GetValue<string>("SYSTEM_OPERATOR_CLIENT_ID") ?? throw new InvalidOperationException("SYSTEM_OPERATOR_CLIENT_ID is not set in configuration");
        var systemOperatorClientSecret = root.GetValue<string>("SYSTEM_OPERATOR_CLIENT_SECRET") ?? throw new InvalidOperationException("SYSTEM_OPERATOR_CLIENT_SECRET is not set in configuration");
        SystemOperator = new Actor(systemOperatorClientId, systemOperatorClientSecret);

        Datahub = new DatahubDsl(EdiDriver);
    }

    public Actor MeteredDataResponsible { get; }

    public Actor SystemOperator { get; }

    public EdiDriver EdiDriver { get; }

    private DatahubDsl Datahub { get; }

    public async Task InitializeAsync()
    {
        // Ensure Queue is empty before starting tests.
        await EmptyQueueForAsync(MeteredDataResponsible, CancellationToken.None).ConfigureAwait(false);
        await EmptyQueueForAsync(SystemOperator, CancellationToken.None).ConfigureAwait(false);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task EmptyQueueForAsync(Actor actor, CancellationToken cancellationToken)
    {
        await Datahub.EmptyQueueForAsync(actor, cancellationToken).ConfigureAwait(false);
    }
}
