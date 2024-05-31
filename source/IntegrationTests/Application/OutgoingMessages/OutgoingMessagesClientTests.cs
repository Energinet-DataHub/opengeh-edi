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
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Xunit;
using HttpClientFactory = Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks.HttpClientFactory;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class OutgoingMessagesClientTests : IAsyncLifetime
{
    private static readonly IntegrationTestConfiguration _integrationTestConfiguration = new();

    public OutgoingMessagesClientTests()
    {
        var calculationIdInTestFile = Guid.Parse("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
        ViewQuery = new EnergyResultPerGridAreaQuery(calculationIdInTestFile);

        // TODO: Refactor "HttpClientFactory" in "TestCommon" to something specific
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
        await DatabricksSchemaManager.CreateTableAsync(ViewQuery);

        var filename = "balance_fixing_01-11-2022_01-12-2022_ga_543.csv";
        var testFilePath = Path.Combine("Application", "OutgoingMessages", "TestData", filename);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(ViewQuery, testFilePath);
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
