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
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents;

[SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "Test method")]
public class GivenCalculationCompletedV1ReceivedForWholesaleFixingTests : WholesaleServicesBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenCalculationCompletedV1ReceivedForWholesaleFixingTests(
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
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerCharges_When_GridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var testMessageData = testDataDescription.ExampleWholesaleResultMessageData;

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridOperator);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCode, gridOperator.ActorNumber);
        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(testDataDescription.CalculationId);

        // When (act)
        var peekResultsForSystemOperatorOperator = await WhenActorPeeksAllMessages(
            systemOperator.ActorNumber,
            systemOperator.ActorRole,
            documentFormat);

        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            documentFormat);

        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            gridOperator.ActorNumber,
            gridOperator.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForSystemOperatorOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount);
        peekResultsForGridOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForGridOwnerCount);
        peekResultsForEnergySupplier.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForEnergySupplierCount);

        var expectedDocumentToSystemOperator = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: systemOperator.ActorNumber.Value,
            ReceiverRole: systemOperator.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: systemOperator.ActorNumber.Value,
            ChargeCode: "Sub-804",
            ChargeType: ChargeType.Subscription,
            Currency: testMessageData.Currency,
            EnergySupplierNumber: testMessageData.EnergySupplier.Value,
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridArea: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        var expectedDocumentToChargeOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: gridOperator.ActorNumber.Value,
            ReceiverRole: gridOperator.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: gridOperator.ActorNumber.Value,
            ChargeCode: "Sub-804",
            ChargeType: ChargeType.Subscription,
            Currency: testMessageData.Currency,
            EnergySupplierNumber: testMessageData.EnergySupplier.Value,
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridArea: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        var expectedDocumentToEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: energySupplier.ActorNumber.Value,
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: gridOperator.ActorNumber.Value,
            ChargeCode: "Sub-804",
            ChargeType: ChargeType.Subscription,
            Currency: testMessageData.Currency,
            EnergySupplierNumber: testMessageData.EnergySupplier.Value,
            SettlementMethod: testMessageData.SettlementMethod,
            MeteringPointType: testMessageData.MeteringPointType,
            GridArea: testMessageData.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    private Task GivenEnqueueWholesaleResultsForAmountPerChargesAsync(Guid calculationId)
    {
        var activity = new EnqueueWholesaleResultsForAmountPerChargesActivity(
            GetService<IServiceScopeFactory>(),
            GetService<IMasterDataClient>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesInput(calculationId, Guid.NewGuid()));
    }

    private async Task<WholesaleResultForAmountPerChargeDescription> GivenDatabricksResultDataForWholesaleResultAmountPerCharge()
    {
        var wholesaleResultForAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        var wholesaleAmountPerChargeQuery = new WholesaleAmountPerChargeQuery(_ediDatabricksOptions.Value,  GetService<IMasterDataClient>(), EventId.From(Guid.NewGuid()), wholesaleResultForAmountPerChargeDescription.CalculationId);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition, wholesaleResultForAmountPerChargeDescription.TestFilePath);
        return wholesaleResultForAmountPerChargeDescription;
    }

    /// <summary>
    /// Assert that one of the messages is correct and don't care about the rest. We have no way of knowing which
    /// message is the correct one, so we will assert all of them and count the number of failed/successful assertions.
    /// </summary>
    private async Task ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
        List<PeekResultDto> peekResults,
        DocumentFormat documentFormat,
        NotifyWholesaleServicesDocumentAssertionInput assertionInput)
    {
        var failedAssertions = new List<Exception>();
        var successfulAssertions = 0;
        foreach (var peekResultDto in peekResults)
        {
            try
            {
                using (new AssertionScope())
                {
                    await ThenNotifyWholesaleServicesDocumentIsCorrect(
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
