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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

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
        await _fixture.InsertWholesaleDataDatabricksDataAsync(_ediDatabricksOptions);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerCharges_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var testMessageData = testDataDescription.ExampleWholesaleResultMessageData;

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridAccessProvider);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCodes.Single(), gridOperator.ActorNumber);

        // TODO: Should we enqueue wholesale results for all actors in the dataset?
        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(testDataDescription.CalculationId, energySupplier, new Dictionary<string, ActorNumber>()
        {
            { "804", gridOperator.ActorNumber },
            { "803", ActorNumber.Create("0000000000000") },
        });

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
        peekResultsForSystemOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount * 2, "because there should be 3 message per grid area.");
        peekResultsForGridOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForGridOwnerCount, "because there should be 3 message per owned grid area.");
        peekResultsForEnergySupplier.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForEnergySupplierCount * 2, "because there should be 3 message per grid area.");

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
            PriceMeasurementUnit: MeasurementUnit.KilowattHour,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForMonthlyAmountPerCharges_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridAccessProvider);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCodes.Single(), gridOperator.ActorNumber);
        await GivenEnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(testDataDescription.CalculationId, energySupplier, new Dictionary<string, ActorNumber>()
        {
            { "804", ActorNumber.Create("8500000000502") },
            { "803", ActorNumber.Create("0000000000000") },
        });

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
        peekResultsForSystemOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForSystemOperatorCount, "because there should be 3 message per grid area.");
        peekResultsForGridOperator.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForGridOwnerCount, "because there should be 3 message per owned grid area.");
        peekResultsForEnergySupplier.Should().HaveCount(testDataDescription.ExpectedOutgoingMessagesForEnergySupplierCount, "because there should be 3 message per grid area.");

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
            PriceMeasurementUnit: MeasurementUnit.KilowattHour,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
            Currency: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.Currency,
            EnergySupplierNumber: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.EnergySupplier.Value,
            SettlementMethod: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.SettlementMethod,
            MeteringPointType: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.MeteringPointType,
            GridArea: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator.Points);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForTotalAmount_When_SystemOperatorAndGridOperatorAndEnergySupplierPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultTotalAmount();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var systemOperator = new Actor(DataHubDetails.SystemOperatorActorNumber, ActorRole.SystemOperator);
        var gridOperator = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridAccessProvider);
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescription.GridAreaCodes.Single(), gridOperator.ActorNumber);
        await GivenEnqueueWholesaleResultsForTotalAmountAsync(testDataDescription.CalculationId, energySupplier, new Dictionary<string, ActorNumber>() { { "804", ActorNumber.Create("8500000000502") } });

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
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Version,
            Resolution: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Resolution,
            Period: testDataDescription.Period,
            Points: testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier.Points);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForSystemOperator,
            documentFormat,
            expectedDocumentToSystemOperator);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForEnergySupplier,
            documentFormat,
            expectedDocumentToEnergySupplier);
    }

    [Fact]
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerChargesWithAGapInFees_When_EnergySupplierPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments()
    {
        // Given (arrange)
        var expectedNumberOfPeekResults = 2;
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);
        await GivenGridAreaOwnershipAsync("804", ActorNumber.Create("8500000000502"));
        var calculationId = Guid.Parse("61d60f89-bbc5-4f7a-be98-6139aab1c1b2");
        var wholesaleAmountPerChargeSchemaDefinition = GetWholesaleAmountPerChargeSchemaDefinition();
        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleAmountPerChargeSchemaDefinition.DataObjectName, wholesaleAmountPerChargeSchemaDefinition.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertAsync(
            wholesaleAmountPerChargeSchemaDefinition.DataObjectName,
            [
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'804'", "'5790001662233'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-01T23:00:00.000+00:00'", "2.000", "NULL", "12.756998", "25.513996"],
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'804'", "'5790001662233'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-02T23:00:00.000+00:00'", "3.000",  "NULL", "12.756998", "38.270994"],
            // "2023-02-03 23:00:00.000000" is missing
            // "2023-02-04 23:00:00.000000" is missing
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'804'", "'5790001662233'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-05T23:00:00.000+00:00'", "1.000", "NULL", "12.756998", "12.756998"],
        ]);

        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(calculationId, energySupplier, new Dictionary<string, ActorNumber>() { { "804", ActorNumber.Create("8500000000502") } });

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            DocumentFormat.Json);

        // Then (assert)
        peekResultsForEnergySupplier.Should().HaveCount(expectedNumberOfPeekResults, "Fee result contains a single gap, which should result in two messages");

        // Assert first fee is correct and within expected period
        var assertForFirstBundle = new AssertNotifyWholesaleServicesJsonDocument(peekResultsForEnergySupplier[0].Bundle);
        assertForFirstBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 1, 23, 0, 0),
                Instant.FromUtc(2023, 2, 3, 23, 0, 0)));
        assertForFirstBundle.HasPoints(
            [
                new WholesaleServicesPoint(1, 2, 12.757m, 25.514m, CalculatedQuantityQuality.Calculated),
                new WholesaleServicesPoint(1, 3, 12.757m, 38.271m, CalculatedQuantityQuality.Calculated),
            ]);

        // Assert second fee is correct and within expected period
        var assertForSecondBundle = new AssertNotifyWholesaleServicesJsonDocument(peekResultsForEnergySupplier[1].Bundle);
        assertForSecondBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 5, 23, 0, 0),
                Instant.FromUtc(2023, 2, 6, 23, 0, 0)));
        assertForSecondBundle.HasPoints(
            [
                new WholesaleServicesPoint(1, 1, 12.757m, 12.757m, CalculatedQuantityQuality.Calculated),
            ]);
    }

    [Fact]
    public async Task AndGiven_EnqueueWholesaleResultsForAmountPerChargesWithMultipleGapsInFees_When_EnergySupplierPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments()
    {
        // Given (arrange)
        var expectedNumberOfPeekResults = 3;
        var energySupplier = new Actor(ActorNumber.Create("5790001662234"), ActorRole.EnergySupplier);
        await GivenGridAreaOwnershipAsync("805", ActorNumber.Create("8500000000502"));
        var calculationId = Guid.Parse("61d60f89-bbc5-4f7a-be98-6139aab1c1b2");
        var wholesaleAmountPerChargeSchemaDefinition = GetWholesaleAmountPerChargeSchemaDefinition();
        await _fixture.DatabricksSchemaManager.CreateTableAsync(wholesaleAmountPerChargeSchemaDefinition.DataObjectName, wholesaleAmountPerChargeSchemaDefinition.SchemaDefinition);
        await _fixture.DatabricksSchemaManager.InsertAsync(
            wholesaleAmountPerChargeSchemaDefinition.DataObjectName,
            [
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'805'", "'5790001662234'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-01T23:00:00.000+00:00'", "2.000", "NULL", "12.756998", "25.513996"],
            // "2023-02-02 23:00:00.000000" is missing
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'805'", "'5790001662234'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-03T23:00:00.000+00:00'", "3.000",  "NULL", "12.756998", "38.270994"],
            // "2023-02-04 23:00:00.000000" is missing
            ["'61d60f89-bbc5-4f7a-be98-6139aab1c1b2'", "'wholesale_fixing'", "'65'", "'3efb1187-f25f-4233-bce6-7e1eaf8f7f68'", "'805'", "'5790001662234'", "'Fee-804'", "'fee'", "'8500000000502'", "'P1D'", "'pcs'", "'consumption'", "'flex'", "'false'", "'DKK'", "'2023-02-05T23:00:00.000+00:00'", "1.000", "NULL", "12.756998", "12.756998"],
        ]);

        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(calculationId, energySupplier, new Dictionary<string, ActorNumber>()
        {
            { "803", ActorNumber.Create("0000000000000") },
            { "804", ActorNumber.Create("0000000000000") },
            { "805", ActorNumber.Create("8500000000502") },
        });

        // When (act)
        var peekResultsForEnergySupplier = await WhenActorPeeksAllMessages(
            energySupplier.ActorNumber,
            energySupplier.ActorRole,
            DocumentFormat.Json);

        // Then (assert)
        peekResultsForEnergySupplier.Should().HaveCount(expectedNumberOfPeekResults, "Each fee should be sent as a separate message");

        // Assert first fee is correct and within expected period
        var assertForFirstBundle = new AssertNotifyWholesaleServicesJsonDocument(peekResultsForEnergySupplier[0].Bundle);
        assertForFirstBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 1, 23, 0, 0),
                Instant.FromUtc(2023, 2, 2, 23, 0, 0)));
        assertForFirstBundle.HasPoints(
            [
                new WholesaleServicesPoint(1, 2, 12.757m, 25.514m, CalculatedQuantityQuality.Calculated),
            ]);

        // Assert second fee is correct and within expected period
        var assertForSecondBundle = new AssertNotifyWholesaleServicesJsonDocument(peekResultsForEnergySupplier[1].Bundle);
        assertForSecondBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 3, 23, 0, 0),
                Instant.FromUtc(2023, 2, 4, 23, 0, 0)));
        assertForSecondBundle.HasPoints(
            [
                new WholesaleServicesPoint(1, 3, 12.757m, 38.271m, CalculatedQuantityQuality.Calculated),
            ]);

        // Assert third fee is correct and within expected period
        var assertForThirdBundle = new AssertNotifyWholesaleServicesJsonDocument(peekResultsForEnergySupplier[2].Bundle);
        assertForThirdBundle.HasPeriod(
            new Period(
                Instant.FromUtc(2023, 2, 5, 23, 0, 0),
                Instant.FromUtc(2023, 2, 6, 23, 0, 0)));
        assertForThirdBundle.HasPoints(
            [
                new WholesaleServicesPoint(1, 1, 12.757m, 12.757m, CalculatedQuantityQuality.Calculated),
            ]);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task
        AndGiven_EnqueueWholesaleResultsForAmountPerCharges_When_TwoGridAreasHasBeenMergedAndGridOwnerPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(
            DocumentFormat documentFormat)
    {
        // Given (arrange)
        var testDataDescriptionForMergedGridArea = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var testMessageDataForMergedGridArea = testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageData;

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var newGridOperatorForMergedGridArea = new Actor(ActorNumber.Create("5790001665533"), ActorRole.GridAccessProvider);
        var oldGridOperatorForMergedGridArea = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridAccessProvider);

        await GivenGridAreaOwnershipAsync(testDataDescriptionForMergedGridArea.GridAreaCodes.Single(), newGridOperatorForMergedGridArea.ActorNumber);

        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);
        await GivenEnqueueWholesaleResultsForAmountPerChargesAsync(
            testDataDescriptionForMergedGridArea.CalculationId,
            energySupplier,
            new Dictionary<string, ActorNumber>
            {
                { "804", newGridOperatorForMergedGridArea.ActorNumber },
                { "803", ActorNumber.Create("0000000000000") },
            });

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            newGridOperatorForMergedGridArea.ActorNumber,
            newGridOperatorForMergedGridArea.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(testDataDescriptionForMergedGridArea.ExpectedOutgoingMessagesForGridOwnerCount, "because there should be 3 message per grid area.");

        var expectedDocumentToGridOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: newGridOperatorForMergedGridArea.ActorNumber.Value,
            ReceiverRole: newGridOperatorForMergedGridArea.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: oldGridOperatorForMergedGridArea.ActorNumber.Value,
            ChargeCode: "Sub-804",
            ChargeType: ChargeType.Subscription,
            Currency: testMessageDataForMergedGridArea.Currency,
            EnergySupplierNumber: testMessageDataForMergedGridArea.EnergySupplier.Value,
            SettlementMethod: testMessageDataForMergedGridArea.SettlementMethod,
            MeteringPointType: testMessageDataForMergedGridArea.MeteringPointType,
            GridArea: testMessageDataForMergedGridArea.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testMessageDataForMergedGridArea.Version,
            Resolution: testMessageDataForMergedGridArea.Resolution,
            Period: testDataDescriptionForMergedGridArea.Period,
            Points: testMessageDataForMergedGridArea.Points);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToGridOwner);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForMonthlyAmountPerCharges_When_TwoGridAreasHasBeenMergedAndGridOwnerPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var newGridOperatorForMergedGridArea = new Actor(ActorNumber.Create("5790001665533"), ActorRole.GridAccessProvider);
        var oldGridOperatorForMergedGridArea = new Actor(ActorNumber.Create("8500000000502"), ActorRole.GridAccessProvider);
        var testDataDescriptionForMergedGridArea = GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescriptionForMergedGridArea.GridAreaCodes.Single(), newGridOperatorForMergedGridArea.ActorNumber);
        await GivenEnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(
            testDataDescriptionForMergedGridArea.CalculationId,
            energySupplier,
            new Dictionary<string, ActorNumber> { { testDataDescriptionForMergedGridArea.GridAreaCodes.Single(), newGridOperatorForMergedGridArea.ActorNumber } });

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            newGridOperatorForMergedGridArea.ActorNumber,
            newGridOperatorForMergedGridArea.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(testDataDescriptionForMergedGridArea.ExpectedOutgoingMessagesForGridOwnerCount);

        var expectedDocumentToGridOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: newGridOperatorForMergedGridArea.ActorNumber.Value,
            ReceiverRole: newGridOperatorForMergedGridArea.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: oldGridOperatorForMergedGridArea.ActorNumber.Value,
            ChargeCode: "Sub-804",
            ChargeType: ChargeType.Subscription,
            Currency: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Currency,
            EnergySupplierNumber: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.EnergySupplier.Value,
            SettlementMethod: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.SettlementMethod,
            MeteringPointType: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.MeteringPointType,
            GridArea: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: MeasurementUnit.Pieces,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.Pieces,
            CalculationVersion: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Version,
            Resolution: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Resolution,
            Period: testDataDescriptionForMergedGridArea.Period,
            Points: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Points);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToGridOwner);
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_EnqueueWholesaleResultsForTotalAmount_When_TwoGridAreasHasBeenMergedAndGridOwnerPeeksMessages_Then_ReceivesCorrectWholesaleServicesDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var newGridOperatorForMergedGridArea = new Actor(ActorNumber.Create("5790001665533"), ActorRole.GridAccessProvider);
        var testDataDescriptionForMergedGridArea = GivenDatabricksResultDataForWholesaleResultTotalAmount();

        GivenNowIs(Instant.FromUtc(2023, 09, 07, 13, 37, 05));
        var energySupplier = new Actor(ActorNumber.Create("5790001662233"), ActorRole.EnergySupplier);

        await GivenGridAreaOwnershipAsync(testDataDescriptionForMergedGridArea.GridAreaCodes.Single(), newGridOperatorForMergedGridArea.ActorNumber);
        await GivenEnqueueWholesaleResultsForTotalAmountAsync(
            testDataDescriptionForMergedGridArea.CalculationId,
            energySupplier,
            new Dictionary<string, ActorNumber> { { testDataDescriptionForMergedGridArea.GridAreaCodes.Single(), newGridOperatorForMergedGridArea.ActorNumber } });

        // When (act)
        var peekResultsForGridOperator = await WhenActorPeeksAllMessages(
            newGridOperatorForMergedGridArea.ActorNumber,
            newGridOperatorForMergedGridArea.ActorRole,
            documentFormat);

        // Then (assert)
        peekResultsForGridOperator.Should().HaveCount(testDataDescriptionForMergedGridArea.ExpectedOutgoingMessagesForGridOwnerCount);

        var expectedDocumentToChargeOwner = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2023-09-07T13:37:05Z",
            BusinessReasonWithSettlementVersion: new(BusinessReason.WholesaleFixing, null),
            ReceiverId: newGridOperatorForMergedGridArea.ActorNumber.Value,
            ReceiverRole: newGridOperatorForMergedGridArea.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value,
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Currency,
            EnergySupplierNumber: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.EnergySupplier.Value,
            SettlementMethod: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.SettlementMethod,
            MeteringPointType: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.MeteringPointType,
            GridArea: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.GridArea,
            OriginalTransactionIdReference: null,
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Version,
            Resolution: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Resolution,
            Period: testDataDescriptionForMergedGridArea.Period,
            Points: testDataDescriptionForMergedGridArea.ExampleWholesaleResultMessageDataForChargeOwner.Points);

        await ThenOneOfWholesaleServicesDocumentsAreCorrect(
            peekResultsForGridOperator,
            documentFormat,
            expectedDocumentToChargeOwner);
    }

    private (string DataObjectName,  Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition) GetWholesaleAmountPerChargeSchemaDefinition()
    {
        var query = new WholesaleAmountPerChargeQuery(
            GetService<ILogger<EnqueueEnergyResultsForBalanceResponsiblesActivity>>(),
            _ediDatabricksOptions.Value,
            ImmutableDictionary<string, ActorNumber>.Empty,
            EventId.From(Guid.NewGuid()),
            Guid.NewGuid(),
            null);
        return new(query.DataObjectName, query.SchemaDefinition);
    }

    private Task GivenEnqueueWholesaleResultsForAmountPerChargesAsync(Guid calculationId, Actor energySupplier, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueWholesaleResultsForAmountPerChargesActivity(
            GetService<ILogger<EnqueueWholesaleResultsForAmountPerChargesActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary(), energySupplier.ActorNumber.Value));
    }

    private Task GivenEnqueueWholesaleResultsForTotalAmountAsync(Guid calculationId, Actor energySupplier, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueWholesaleResultsForTotalAmountsActivity(
            GetService<ILogger<EnqueueWholesaleResultsForTotalAmountsActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary(), energySupplier.ActorNumber.Value));
    }

    private Task GivenEnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(Guid calculationId, Actor energySupplier, IDictionary<string, ActorNumber> gridAreaOwners)
    {
        var activity = new EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity(
            GetService<ILogger<EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity>>(),
            GetService<IServiceScopeFactory>(),
            GetService<WholesaleResultEnumerator>());

        return activity.Run(new EnqueueMessagesForActorInput(calculationId, Guid.NewGuid(), gridAreaOwners.ToImmutableDictionary(), energySupplier.ActorNumber.Value));
    }

    /// <summary>
    /// Assert that one of the messages is correct and don't care about the rest. We have no way of knowing which
    /// message is the correct one, so we will assert all of them and count the number of failed/successful assertions.
    /// </summary>
    private async Task ThenOneOfWholesaleServicesDocumentsAreCorrect(
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
