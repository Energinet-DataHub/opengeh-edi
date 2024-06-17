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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

[SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "Test method")]
public class GivenCalculationCompletedV1ReceivedTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenCalculationCompletedV1ReceivedTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
            : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public async Task InitializeAsync()
    {
        await _fixture.DatabricksSchemaManager.CreateSchemaAsync();
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
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerGridAreaAsync();

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var gridOperator = new Actor(ActorNumber.Create("1111111111111"), ActorRole.GridOperator);
        var gridArea = testDataDescription.GridAreaCode;
        var calculationId = testDataDescription.CalculationId;

        await GivenGridAreaOwnershipAsync(gridArea, gridOperator.ActorNumber);
        await GivenEnqueueEnergyResultsForGridAreaOwnersAsync(calculationId);

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            gridOperator.ActorNumber,
            gridOperator.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesCount);

        // TODO: Assert correct document content
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_CalculationIsBalanceFixing_WhenBalanceResponsiblePeeksMessages_ThenReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerBalanceResponsible();
        var (
            balanceResponsibleActorNumber,
            expectedMessagesCount,
            exampleMessageData) = testDataDescription.ExampleBalanceResponsible;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var balanceResponsible = new Actor(balanceResponsibleActorNumber, ActorRole.BalanceResponsibleParty);
        var calculationId = testDataDescription.CalculationId;
        var expectedPeriod = testDataDescription.Period;

        await GivenEnqueueEnergyResultsForBalanceResponsiblesAsync(calculationId);

        // When (act)
        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForBalanceResponsible.Should().HaveCount(expectedMessagesCount);

        // => Add "NotBeNull()" assertions for each expected message except one, since there is no .AnySatisfy()
        // method and we only want to assert that one of the messages is correct.
        var assertions = Enumerable
            .Repeat<Action<PeekResultDto>>(r => r.Should().NotBeNull(), expectedMessagesCount - 1)
            .ToList();

        assertions.Add(
            async r =>
            {
                var assertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
                    Timestamp: "2022-09-07T13:37:05Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.BalanceFixing,
                        null),
                    ReceiverId: balanceResponsible.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: null,
                    BalanceResponsibleNumber: balanceResponsible.ActorNumber,
                    SettlementMethod: exampleMessageData.SettlementMethod,
                    MeteringPointType: exampleMessageData.MeteringPointType,
                    GridAreaCode: exampleMessageData.GridArea,
                    OriginalTransactionIdReference: null,
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: exampleMessageData.Version,
                    Resolution: exampleMessageData.Resolution,
                    Period: expectedPeriod,
                    Points: exampleMessageData.Points);

                await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                    r.Bundle,
                    documentFormat,
                    assertionInput);
            });

        peekResultsForBalanceResponsible.Should().SatisfyRespectively(assertions);
    }

    private Task GivenEnqueueEnergyResultsForGridAreaOwnersAsync(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForGridAreaOwnersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private Task GivenEnqueueEnergyResultsForBalanceResponsiblesAsync(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private async Task<EnergyResultPerGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerGridAreaAsync()
    {
        var energyResultPerGridAreaTestDataDescription = new EnergyResultPerGridAreaDescription();
        var energyResultPerGridAreaQuery = new EnergyResultPerGridAreaQuery(_ediDatabricksOptions.Value, energyResultPerGridAreaTestDataDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerGridAreaQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerGridAreaQuery, energyResultPerGridAreaTestDataDescription.TestFilePath);
        return energyResultPerGridAreaTestDataDescription;
    }

    private async Task<EnergyResultPerBrpGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerBalanceResponsible()
    {
        var energyResultPerBrpDescription = new EnergyResultPerBrpGridAreaDescription();
        var energyResultPerBrpQuery = new EnergyResultPerBrpGridAreaQuery(_ediDatabricksOptions.Value, energyResultPerBrpDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerBrpQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerBrpQuery, energyResultPerBrpDescription.TestFilePath);
        return energyResultPerBrpDescription;
    }

    private async Task<EnergyResultPerEnergySupplierBrpGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerEnergySupplierAsync()
    {
        var energyResultPerEnergySupplierDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var energyResultPerEnergySupplierQuery = new EnergyResultPerEnergySupplierBrpGridAreaQuery(_ediDatabricksOptions.Value, energyResultPerEnergySupplierDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerEnergySupplierQuery, energyResultPerEnergySupplierDescription.TestFilePath);
        return energyResultPerEnergySupplierDescription;
    }
}
