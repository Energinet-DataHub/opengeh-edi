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
    public async Task AndGiven_EnqueueEnergyResultsForGridOperators_When_GridOperatorPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerGridArea();
        var expectedMessagesCount = testDataDescription.ExpectedOutgoingMessagesCount;
        var expectedPeriod = testDataDescription.Period;
        var exampleMessageData = testDataDescription.ExampleMessageData;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var gridOperator = new Actor(ActorNumber.Create("1111111111111"), ActorRole.GridOperator);
        var gridArea = testDataDescription.GridAreaCode;
        var calculationId = testDataDescription.CalculationId;

        await GivenGridAreaOwnershipAsync(gridArea, gridOperator.ActorNumber);
        await GivenEnqueueEnergyResultsForGridOperatorsAsync(calculationId);

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            gridOperator.ActorNumber,
            gridOperator.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(expectedMessagesCount);

        var assertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: gridOperator.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
            // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: null,
            BalanceResponsibleNumber: null,
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

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsForBalanceResponsibles_When_BalanceResponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
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

        await GivenEnqueueEnergyResultsForBalanceResponsibles(calculationId);

        // When (act)
        var peekResultsForBalanceResponsible = await WhenActorPeeksAllMessages(
            balanceResponsible.ActorNumber,
            balanceResponsible.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForBalanceResponsible.Should().HaveCount(expectedMessagesCount);

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

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            assertionInput);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueEnergyResultsForEnergySuppliers_When_EnergySupplierAndBalanceReponsiblePeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var (
            energySupplierActorNumber,
            energySupplierExpectedMessagesCount,
            energySupplierExampleMessageData) = testDataDescription.ExampleEnergySupplier;

        var (
            balanceResponsibleActorNumber,
            balanceResponsibleExpectedMessagesCount,
            balanceResponsibleExampleMessageData) = testDataDescription.ExampleBalanceResponsible;

        GivenNowIs(Instant.FromUtc(2022, 09, 07, 13, 37, 05));
        var energySupplier = new Actor(energySupplierActorNumber, ActorRole.EnergySupplier);
        var balanceResponsible = new Actor(balanceResponsibleActorNumber, ActorRole.BalanceResponsibleParty);
        var calculationId = testDataDescription.CalculationId;
        var expectedPeriod = testDataDescription.Period;

        await GivenEnqueueEnergyResultsForEnergySuppliers(calculationId);

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
        peekResultsForEnergySupplier.Should().HaveCount(energySupplierExpectedMessagesCount);

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
            SettlementMethod: energySupplierExampleMessageData.SettlementMethod,
            MeteringPointType: energySupplierExampleMessageData.MeteringPointType,
            GridAreaCode: energySupplierExampleMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: energySupplierExampleMessageData.Version,
            Resolution: energySupplierExampleMessageData.Resolution,
            Period: expectedPeriod,
            Points: energySupplierExampleMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            energySupplierAssertionInput);

        // Assert balance responsible peek result
        peekResultsForBalanceResponsible.Should().HaveCount(balanceResponsibleExpectedMessagesCount);

        var balanceResponsibleAssertionInput = new NotifyAggregatedMeasureDataDocumentAssertionInput(
            Timestamp: "2022-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.BalanceFixing,
                null),
            ReceiverId: balanceResponsible.ActorNumber,
            // ReceiverRole: originalActor.ActorRole,
            SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
            // SenderRole: ActorRole.MeteredDataAdministrator,
            EnergySupplierNumber: balanceResponsibleExampleMessageData.EnergySupplier,
            BalanceResponsibleNumber: balanceResponsible.ActorNumber,
            SettlementMethod: balanceResponsibleExampleMessageData.SettlementMethod,
            MeteringPointType: balanceResponsibleExampleMessageData.MeteringPointType,
            GridAreaCode: balanceResponsibleExampleMessageData.GridArea,
            OriginalTransactionIdReference: null,
            ProductCode: ProductType.EnergyActive.Code,
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: balanceResponsibleExampleMessageData.Version,
            Resolution: balanceResponsibleExampleMessageData.Resolution,
            Period: expectedPeriod,
            Points: balanceResponsibleExampleMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForBalanceResponsible,
            documentFormat,
            balanceResponsibleAssertionInput);
    }

    private Task GivenEnqueueEnergyResultsForGridOperatorsAsync(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForGridAreaOwnersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private Task GivenEnqueueEnergyResultsForBalanceResponsibles(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private Task GivenEnqueueEnergyResultsForEnergySuppliers(Guid calculationId)
    {
        var activity = new EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
            GetService<IOutgoingMessagesClient>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private async Task<(
        EnergyResultPerGridAreaDescription PerGridArea,
        EnergyResultPerBrpGridAreaDescription PerBalanceResponsible,
        EnergyResultPerEnergySupplierBrpGridAreaDescription PerEnergySupplier)> GivenDatabricksResultDataForAllAggregationViews()
    {
        var perGridArea = await GivenDatabricksResultDataForEnergyResultPerGridArea();
        var perBalanceResponsible = await GivenDatabricksResultDataForEnergyResultPerBalanceResponsible();
        var perEnergySupplier = await GivenDatabricksResultDataForEnergyResultPerEnergySupplier();

        return (perGridArea, perBalanceResponsible, perEnergySupplier);
    }

    private async Task<EnergyResultPerGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerGridArea()
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

    private async Task<EnergyResultPerEnergySupplierBrpGridAreaDescription> GivenDatabricksResultDataForEnergyResultPerEnergySupplier()
    {
        var energyResultPerEnergySupplierDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var energyResultPerEnergySupplierQuery = new EnergyResultPerEnergySupplierBrpGridAreaQuery(_ediDatabricksOptions.Value, energyResultPerEnergySupplierDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(energyResultPerEnergySupplierQuery);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(energyResultPerEnergySupplierQuery, energyResultPerEnergySupplierDescription.TestFilePath);
        return energyResultPerEnergySupplierDescription;
    }

    private async Task ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
        List<PeekResultDto> peekResults,
        DocumentFormat documentFormat,
        NotifyAggregatedMeasureDataDocumentAssertionInput assertionInput)
    {
        // We need to assert that one of the messages is correct and don't care about the rest. However we have no
        // way of knowing which message is the correct one, so we will assert all of them and count the number of
        // failed/successful assertions.
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
