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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

public class GivenCalculationCompletedV1ReceivedTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly EnergyResultPerGridAreaDescription _energyResultPerGridAreaTestDataDescription;

    public GivenCalculationCompletedV1ReceivedTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
            : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        _energyResultPerGridAreaTestDataDescription = new EnergyResultPerGridAreaDescription();
    }

    public async Task InitializeAsync()
    {
        await _fixture.DatabricksSchemaManager.CreateSchemaAsync();
        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();

        var energyResultPerGridAreaQuery = new EnergyResultPerGridAreaQuery(ediDatabricksOptions.Value, _energyResultPerGridAreaTestDataDescription.CalculationId);

        await SeedDatabricksWithDataAsync(_energyResultPerGridAreaTestDataDescription, energyResultPerGridAreaQuery);
    }

    public async Task DisposeAsync()
    {
        await _fixture.DatabricksSchemaManager.DropSchemaAsync();
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_CalculationIsBalanceFixing_WhenGridOperatorPeeksMessages_ThenReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var gridOperator = new Actor(ActorNumber.Create("1111111111111"), ActorRole.GridOperator);
        var gridArea = _energyResultPerGridAreaTestDataDescription.GridAreaCode;
        var calculationId = _energyResultPerGridAreaTestDataDescription.CalculationId;

        await GivenGridAreaOwnershipAsync(gridArea, gridOperator.ActorNumber);
        await GivenEnqueueEnergyResultsForGridAreaOwnersAsync(calculationId);

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            gridOperator.ActorNumber,
            gridOperator.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(_energyResultPerGridAreaTestDataDescription.ExpectedOutgoingMessagesCount);

        // TODO: Assert correct document content
    }

    private Task GivenEnqueueEnergyResultsForGridAreaOwnersAsync(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForGridAreaOwnersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private async Task SeedDatabricksWithDataAsync(EnergyResultTestDataDescription testDataDescription, IDeltaTableSchemaDescription schemaInfomation)
    {
        await _fixture.DatabricksSchemaManager.CreateTableAsync(schemaInfomation);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(schemaInfomation, testDataDescription.TestFilePath);
    }
}
