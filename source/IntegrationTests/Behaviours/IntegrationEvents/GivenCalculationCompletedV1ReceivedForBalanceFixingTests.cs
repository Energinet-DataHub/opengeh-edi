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
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

[SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "Test method")]
public class GivenCalculationCompletedV1ReceivedForBalanceFixingTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenCalculationCompletedV1ReceivedForBalanceFixingTests(
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
    public async Task AndGiven_EnqueueEnergyResultsPerGridArea_When_MeteredDataResponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerGridArea();
        var testMessageData = testDataDescription.ExampleEnergyResultMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var meteredDataResponsible = new Actor(ActorNumber.Create("1111111111111"), ActorRole.MeteredDataResponsible);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCode, meteredDataResponsible.ActorNumber);
        await GivenEnqueueEnergyResultsPerGridAreaAsync(testDataDescription.CalculationId);

        // When (act)
        var peekResultsForMeteredDataResponsible = await WhenActorPeeksAllMessages(
            meteredDataResponsible.ActorNumber,
            meteredDataResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForMeteredDataResponsible.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesCount);

        var assertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: meteredDataResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
            // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: null,
            BalanceResponsibleNumber: null,
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridAreaCode: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForMeteredDataResponsible,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsPerBalanceResponsible_When_BalanceResponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerBalanceResponsible();
        var testMessageData = testDataDescription.ExampleBalanceResponsible.ExampleMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var balanceResponsible = new Actor(
            testDataDescription.ExampleBalanceResponsible.ActorNumber,
            ActorRole.BalanceResponsibleParty);

        await GivenEnqueueEnergyResultsPerBalanceResponsible(testDataDescription.CalculationId);

        // When (act)
        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForBalanceResponsible.Should().HaveCount(testDataDescription.ExampleBalanceResponsible.ExpectedOutgoingMessagesCount);

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
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridAreaCode: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible_When_EnergySupplierAndBalanceReponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var energySupplierTestMessageData = testDataDescription.ExampleEnergySupplier.ExampleMessageData;
        var balanceResponsibleTestMessageData = testDataDescription.ExampleBalanceResponsible.ExampleMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var energySupplier = new Actor(testDataDescription.ExampleEnergySupplier.ActorNumber, ActorRole.EnergySupplier);
        var balanceResponsible = new Actor(testDataDescription.ExampleBalanceResponsible.ActorNumber, ActorRole.BalanceResponsibleParty);

        await GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(testDataDescription.CalculationId);

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            documentFormat);

        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)

        // Assert energy supplier peek result
        peekResultsForEnergySupplier
            .Should()
            .HaveCount(testDataDescription.ExampleEnergySupplier.ExpectedOutgoingMessagesCount);

        var energySupplierAssertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: energySupplier.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
            // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: energySupplier.ActorNumber,
            BalanceResponsibleNumber: null,
            SettlementMethod: energySupplierTestMessageData.SettlementMethod,
            MeteringPointType: energySupplierTestMessageData.MeteringPointType,
            GridAreaCode: energySupplierTestMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: energySupplierTestMessageData.Version,
            Resolution: energySupplierTestMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: energySupplierTestMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            energySupplierAssertionInput);

        // Assert balance responsible peek result
        peekResultsForBalanceResponsible
            .Should()
            .HaveCount(testDataDescription.ExampleBalanceResponsible.ExpectedOutgoingMessagesCount);

        var balanceResponsibleAssertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: balanceResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
            // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: balanceResponsibleTestMessageData.EnergySupplier,
            BalanceResponsibleNumber: balanceResponsible.ActorNumber,
            SettlementMethod: balanceResponsibleTestMessageData.SettlementMethod,
            MeteringPointType: balanceResponsibleTestMessageData.MeteringPointType,
            GridAreaCode: balanceResponsibleTestMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: balanceResponsibleTestMessageData.Version,
            Resolution: balanceResponsibleTestMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: balanceResponsibleTestMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            balanceResponsibleAssertionInput);
    }

    private Task GivenEnqueueEnergyResultsPerGridAreaAsync(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForGridAreaOwnersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private Task GivenEnqueueEnergyResultsPerBalanceResponsible(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private Task GivenEnqueueEnergyResultsPerEnergySuppliersPerBalanceResponsible(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private async Task<EnergyResultPerGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerGridArea()
    {
        var energyResultPerGridAreaTestDataDescription = new EnergyResultPerGridAreaDescription();
        var energyResultPerGridAreaQuery = new EnergyResultPerGridAreaQuery(_ediDatabricksOptions.Value,  GetService<IMasterDataClient>(), EventId.From(Guid.NewGuid()), energyResultPerGridAreaTestDataDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerGridAreaQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerGridAreaQuery, energyResultPerGridAreaTestDataDescription.TestFilePath);
        return energyResultPerGridAreaTestDataDescription;
    }

    private async Task<EnergyResultPerBrpGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerBalanceResponsible()
    {
        var energyResultPerBrpDescription = new EnergyResultPerBrpGridAreaDescription();
        var energyResultPerBrpQuery = new EnergyResultPerBrpGridAreaQuery(_ediDatabricksOptions.Value, EventId.From(Guid.NewGuid()), energyResultPerBrpDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerBrpQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerBrpQuery, energyResultPerBrpDescription.TestFilePath);
        return energyResultPerBrpDescription;
    }

    private async Task<EnergyResultPerEnergySupplierBrpGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerEnergySupplier()
    {
        var energyResultPerEnergySupplierDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var energyResultPerEnergySupplierQuery = new EnergyResultPerEnergySupplierBrpGridAreaQuery(_ediDatabricksOptions.Value, EventId.From(Guid.NewGuid()), energyResultPerEnergySupplierDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerEnergySupplierQuery, energyResultPerEnergySupplierDescription.TestFilePath);
        return energyResultPerEnergySupplierDescription;
    }

    /// <summary>
    /// Assert that one of the messages is correct and don't care about the rest. We have no way of knowing which
    /// message is the correct one, so we will assert all of them and count the number of failed/successful assertions.
    /// </summary>
    private async Task ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
        List<PeekResultDto> peekResults,
        DocumentFormat documentFormat,
        NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        var failedAssertions = new List<Exception>();
        var successfulAssertions = 0;
        foreach (var peekResultDto in peekResults)
        {
            try
            {
                using (new AssertionScope())
                {
                    await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                        peekResultDto.Bundle,
                        documentFormat,
                        assertionInput);
                }

                successfulAssertions++;
            }
            catch (Exception e)
            {
                failedAssertions.Add(e);
            }
        }

        failedAssertions.Should().HaveCount(peekResults.Count - 1);
        successfulAssertions.Should().Be(1);
    }
}
