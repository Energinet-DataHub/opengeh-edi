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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

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
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerCharges_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var testMessageData = testDataDescription.ExampleWholesaleResultMessageData;

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridOperator);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCode, gridOperator.ActorNumber);

        // TODO: Should we enqueue wholesale results for all actors in the dataset?
        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(testDataDescription.CalculationId, energySupplier);

        // When (act)
        var peekResultsForSystemOperator = await WhenActorPeeksAllMessages(
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
        peekResultsForSystemOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount);
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
            ChargeCode: "41000",
            ChargeType: ChargeType.Tariff,
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Points);

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
            PriceMeasurementUnit: MeasurementUnit.Pieces,
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
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testMessageData.Version,
            Resolution: testMessageData.Resolution,
            Period: testDataDescription.Period,
            Points: testMessageData.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForMonthlyAmountPerCharges_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridOperator);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCode, gridOperator.ActorNumber);
        await GivenEnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(testDataDescription.CalculationId, energySupplier);

        // When (act)
        var peekResultsForSystemOperator = await WhenActorPeeksAllMessages(
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
        peekResultsForSystemOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount);
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
            ChargeCode: "45013",
            ChargeType: ChargeType.Tariff,
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Points);

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
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Points);

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
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForTotalAmount_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectNotifyAggregatedMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = await GivenDatabricksResultDataForWholesaleResultTotalAmount();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridOperator);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCode, gridOperator.ActorNumber);
        await GivenEnqueueWholesaleResultsForTotalAmountAsync(testDataDescription.CalculationId, energySupplier);

        // When (act)
        var peekResultsForSystemOperator = await WhenActorPeeksAllMessages(
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
        peekResultsForSystemOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount);
        peekResultsForGridOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForGridOwnerCount);
        peekResultsForEnergySupplier.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForEnergySupplierCount);

        var expectedDocumentToSystemOperator = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: systemOperator.ActorNumber.Value,
            ReceiverRole: systemOperator.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator.Points);

        var expectedDocumentToChargeOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: gridOperator.ActorNumber.Value,
            ReceiverRole: gridOperator.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForChargeOwner.Points);

        var expectedDocumentToEnergySupplier = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: energySupplier.ActorNumber.Value,
            ReceiverRole: ActorRole.EnergySupplier,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Points);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfNotifyAggregatedMeasureDataDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    private Task GivenEnqueueWholesaleResultsForAmountPerChargesAsync(Guid calculationId, Actor energySupplier)
    {
        var activity = new EnqueueWholesaleResultsForAmountPerChargesActivity(
            GetService<ILogger<EnqueueWholesaleResultsForAmountPerChargesActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<IMasterDataClient>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), energySupplier.ActorNumber.Value));
    }

    private Task GivenEnqueueWholesaleResultsForTotalAmountAsync(Guid calculationId, Actor energySupplier)
    {
        var activity = new EnqueueWholesaleResultsForTotalAmountsActivity(
            GetService<ILogger<EnqueueWholesaleResultsForTotalAmountsActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), energySupplier.ActorNumber.Value));
    }

    private Task GivenEnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(Guid calculationId, Actor energySupplier)
    {
        var activity = new EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity(
            GetService<ILogger<EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<IMasterDataClient>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), energySupplier.ActorNumber.Value));
    }

    private async Task<WholesaleResultForAmountPerChargeDescription> GivenDatabricksResultDataForWholesaleResultAmountPerCharge()
    {
        var wholesaleResultForAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        var wholesaleAmountPerChargeQuery = new WholesaleAmountPerChargeQuery(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            _ediDatabricksOptions.Value,
            GetService<IMasterDataClient>(),
            EventId.From(Guid.NewGuid()),
            wholesaleResultForAmountPerChargeDescription.CalculationId,
            null);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleAmountPerChargeQuery.DataObjectName, wholesaleAmountPerChargeQuery.SchemaDefinition, wholesaleResultForAmountPerChargeDescription.TestFilePath);
        return wholesaleResultForAmountPerChargeDescription;
    }

    private async Task<WholesaleResultForMonthlyAmountPerChargeDescription> GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge()
    {
        var wholesaleResultForMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
        var wholesaleMonthlyAmountPerChargeQuery = new WholesaleMonthlyAmountPerChargeQuery(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            _ediDatabricksOptions.Value,
            GetService<IMasterDataClient>(),
            EventId.From(Guid.NewGuid()),
            wholesaleResultForMonthlyAmountPerChargeDescription.CalculationId,
            null);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleMonthlyAmountPerChargeQuery.DataObjectName, wholesaleMonthlyAmountPerChargeQuery.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleMonthlyAmountPerChargeQuery.DataObjectName, wholesaleMonthlyAmountPerChargeQuery.SchemaDefinition, wholesaleResultForMonthlyAmountPerChargeDescription.TestFilePath);
        return wholesaleResultForMonthlyAmountPerChargeDescription;
    }

    private async Task<WholesaleResultForTotalAmountDescription> GivenDatabricksResultDataForWholesaleResultTotalAmount()
    {
        var resultDataForWholesaleResultTotalAmount = new WholesaleResultForTotalAmountDescription();
        var wholesaleTotalAmountQuery = new WholesaleTotalAmountQuery(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            _ediDatabricksOptions.Value,
            EventId.From(Guid.NewGuid()),
            resultDataForWholesaleResultTotalAmount.CalculationId,
            null);

        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleTotalAmountQuery.DataObjectName, wholesaleTotalAmountQuery.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(wholesaleTotalAmountQuery.DataObjectName, wholesaleTotalAmountQuery.SchemaDefinition, resultDataForWholesaleResultTotalAmount.TestFilePath);
        return resultDataForWholesaleResultTotalAmount;
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
