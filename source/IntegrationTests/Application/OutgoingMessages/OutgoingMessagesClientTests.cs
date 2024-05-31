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

using Dapper;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class OutgoingMessagesClientTests : TestBase, IAsyncLifetime, IClassFixture<IntegrationTestFixture>
{
    /// <summary>
    /// Located in 'Application\OutgoingMessages\TestData'
    /// </summary>
    private const string TestFilename = "balance_fixing_01-11-2022_01-12-2022_ga_543.csv";

    // Values matching test file values
    private readonly Guid _calculationId = Guid.Parse("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
    private readonly int _calculationVersion = 63;

    public OutgoingMessagesClientTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public async Task InitializeAsync()
    {
        await DatabricksSchemaManager.CreateSchemaAsync();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        ////Options.Create(new EdiDatabricksOptions { DatabaseName = DatabricksSchemaManager.SchemaName });
        var viewQuery = new EnergyResultPerGridAreaQuery(ediDatabricksOptions, _calculationId);
        await DatabricksSchemaManager.CreateTableAsync(viewQuery);

        var testFilePath = Path.Combine("Application", "OutgoingMessages", "TestData", TestFilename);
        await DatabricksSchemaManager.InsertFromCsvFileAsync(viewQuery, testFilePath);
    }

    public async Task DisposeAsync()
    {
        await DatabricksSchemaManager.DropSchemaAsync();
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueByCalculationId_ThenOutgoingMessagesAreEnqueued()
    {
        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            _calculationId,
            _calculationVersion,
            EventId: Guid.NewGuid());

        // Act
        await sut.EnqueueByCalculationIdAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await
            connection
                .QueryAsync(sql);

        var actualCount = result;
    }
}
