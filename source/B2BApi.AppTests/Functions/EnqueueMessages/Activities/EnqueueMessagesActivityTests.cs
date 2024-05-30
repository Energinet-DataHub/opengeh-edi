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
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Infrastructure.SqlStatements.Queries.EnergyResult;
using Xunit;
using HttpClientFactory = Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks.HttpClientFactory;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.Activities;

public class EnqueueMessagesActivityTests : IAsyncLifetime
{
    private static readonly IntegrationTestConfiguration _integrationTestConfiguration = new();

    public EnqueueMessagesActivityTests()
    {
        var calculationId = Guid.NewGuid();
        ViewQuery = new EnergyResultPerGridAreaQuery(calculationId);
        DatabricksSchemaManager = new DatabricksSchemaManager(
            new HttpClientFactory(),
            databricksSettings: _integrationTestConfiguration.DatabricksSettings,
            schemaPrefix: ViewQuery.DatabaseName);
    }

    private EnergyResultPerGridAreaQuery ViewQuery { get; }

    private DatabricksSchemaManager DatabricksSchemaManager { get; }

    public async Task InitializeAsync()
    {
        await DatabricksSchemaManager.CreateSchemaAsync();

        // TODO: Refactor DatabricksSchemaManager.CreateTableAsync to use IReadOnlyDictionary for column definition
        await DatabricksSchemaManager.CreateTableAsync(
            ViewQuery.DataObjectName,
            ViewQuery.SchemaDefinition);
    }

    public async Task DisposeAsync()
    {
        await DatabricksSchemaManager.DropSchemaAsync();
    }

    [Fact]
    public async Task GivenX_WhenY_ThenZ()
    {
        // Act
        await Task.Delay(3000);
    }
}
