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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenAggregatedMeasureDataV2RequestTests : AggregatedMeasureDataBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenAggregatedMeasureDataV2RequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _fixture = integrationTestFixture;
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.UseRequestAggregatedMeasureDataProcessOrchestration, true);
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public static object[][] DocumentFormatsWithActorRoleCombinationsForNullGridArea() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: true);

    public static object[][] DocumentFormatsWithAllActorRoleCombinations() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: false);

    public static object[][] DocumentFormatsWithActorRoleCombinations(bool nullGridArea)
    {
        // The actor roles who can perform AggregatedMeasureDataRequest's
        var actorRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.BalanceResponsibleParty,
        };

        if (!nullGridArea)
        {
            actorRoles.Add(ActorRole.MeteredDataResponsible);
            actorRoles.Add(ActorRole.GridAccessProvider); // Grid Operator can make requests because of DDM -> MDR hack
        }

        var incomingDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(except: new[]
            {
                DocumentFormat.Ebix.Name, // ebIX is not supported for requests
            })
            .ToArray();

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return actorRoles
            .SelectMany(actorRole => incomingDocumentFormats
                .SelectMany(incomingDocumentFormat => peekDocumentFormats
                    .Select(peekDocumentFormat => new object[]
                    {
                        actorRole,
                        incomingDocumentFormat,
                        peekDocumentFormat,
                    })))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyAggregatedMeasureDataDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var testMessageData = actorRole == ActorRole.EnergySupplier
         ? testDataDescription.ExampleEnergySupplier
         : testDataDescription.ExampleBalanceResponsible;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = testMessageData.ExampleMessageData.BalanceResponsible;
        var actor = (ActorNumber: actorRole == ActorRole.EnergySupplier
            ? energySupplierNumber!
                : balanceResponsibleParty!, ActorRole: actorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (testMessageData.ExampleMessageData.GridArea, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber!.Value,
                balanceResponsibleParty!.Value,
                new List<string> { testMessageData.ExampleMessageData.GridArea },
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesAccepted = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedEnergyTimeSeriesInput);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(requestCalculatedEnergyTimeSeriesAccepted);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle("because there should be one message when requesting for one grid area")
                .Subject;
        }

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: actor.ActorNumber,
                SenderId: DataHubDetails.DataHubActorNumber,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                GridAreaCode: testMessageData.ExampleMessageData.GridArea,
                OriginalTransactionIdReference: transactionId,
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: testMessageData.ExampleMessageData.Version,
                Resolution: testMessageData.ExampleMessageData.Resolution,
                Period: new Period(
                    CreateDateInstant(2022, 01, 12),
                    CreateDateInstant(2022, 01, 13)),
                Points: testMessageData.ExampleMessageData.Points));
    }

    [Theory(Skip = "not updated yet")]
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinationsForNullGridArea))]
    public async Task AndGiven_DataInTwoGridAreas_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyAggregatedMeasureDataDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierNumber = currentActor.ActorRole == ActorRole.EnergySupplier
            ? currentActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var balanceResponsibleParty = currentActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? currentActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: currentActor.ActorNumber,
            senderActorRole: currentActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("12356478912356478912356478912356478")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy: senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>(),
                RequestedForActorNumber: currentActor.ActorNumber.Value,
                RequestedForActorRole: currentActor.ActorRole.Name,
                EnergySupplier: energySupplierNumber.Value,
                BalanceResponsibleParty: balanceResponsibleParty.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock AggregatedTimeSeriesRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var generateDataInGridAreas = new List<string> { "106", "509" };
        var aggregatedMeasureDataRequestAcceptedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow(), generateDataInGridAreas);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, aggregatedMeasureDataRequestAcceptedMessage);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            currentActor.ActorNumber,
            currentActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            peekResults.Should().HaveSameCount(generateDataInGridAreas, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in peekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            var seriesRequest = aggregatedMeasureDataRequestAcceptedMessage.Series
                .Should().ContainSingle(request => request.GridArea == peekResultGridArea)
                .Subject;

            await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyAggregatedMeasureDataDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.BalanceFixing,
                        null),
                    ReceiverId: currentActor.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: seriesRequest.GridArea,
                    OriginalTransactionIdReference: TransactionId.From("12356478912356478912356478912356478"),
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: TimeSeriesPointAssertionInput.From(seriesRequest.TimeSeriesPoints)));
        }

        resultGridAreas.Should().BeEquivalentTo("106", "509");
    }

    [Theory(Skip = "not updated yet")]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_RequestHasNoDataInOptionalFields_When_ActorPeeksAllMessages_Then_ReceivesNotifyAggregatedMeasureDataDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierOrNull = currentActor.ActorRole == ActorRole.EnergySupplier
            ? currentActor.ActorNumber
            : null;
        var balanceResponsibleOrNull = currentActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? currentActor.ActorNumber
            : null;
        var gridAreaOrNull = currentActor.ActorRole == ActorRole.GridAccessProvider || currentActor.ActorRole == ActorRole.MeteredDataResponsible
            ? "512"
            : null;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: currentActor.ActorNumber,
            senderActorRole: currentActor.ActorRole,
            meteringPointType: null,
            settlementMethod: null,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierOrNull,
            balanceResponsibleParty: balanceResponsibleOrNull,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaOrNull, TransactionId.From("12356478912356478912356478912356478")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy: senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: gridAreaOrNull != null ? new List<string> { gridAreaOrNull } : new List<string>(),
                RequestedForActorNumber: currentActor.ActorNumber.Value,
                RequestedForActorRole: currentActor.ActorRole.Name,
                EnergySupplier: energySupplierOrNull?.Value,
                BalanceResponsibleParty: balanceResponsibleOrNull?.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null,
                SettlementMethod: null,
                MeteringPointType: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock AggregatedTimeSeriesRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = gridAreaOrNull == null ? new List<string> { "512" } : null;
        var acceptedResponse = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow(), defaultGridAreas);

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, acceptedResponse);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            currentActor.ActorNumber,
            currentActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle("because there should be one message when requesting for one grid area")
                .Subject;
        }

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: currentActor.ActorNumber,
                // ReceiverRole: originalActor.ActorRole,
                SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                // SenderRole: ActorRole.MeteredDataAdministrator,
                EnergySupplierNumber: energySupplierOrNull,
                BalanceResponsibleNumber: balanceResponsibleOrNull,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridAreaCode: "512",
                OriginalTransactionIdReference: TransactionId.From("12356478912356478912356478912356478"),
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: TimeSeriesPointAssertionInput.From(acceptedResponse.Series.Single().TimeSeriesPoints)));
    }

    [Theory(Skip = "not updated yet")]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_RequestedThreeSeries_When_ActorPeeksAllMessages_Then_ReceivesThreeNotifyAggregatedMeasureDataDocumentsWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierNumber = currentActor.ActorRole == ActorRole.EnergySupplier
            ? currentActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var balanceResponsibleParty = currentActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? currentActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        var gridAreasWithTransactionId = new (string? GridArea, TransactionId TransactionId)[]
        {
            ("143", TransactionId.From("12356478912356478912356478912356478")),
            ("512", TransactionId.From("12356478912356478912356478912356479")),
            ("877", TransactionId.From("12356478912356478912356478912356470")),
        };

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: currentActor.ActorNumber,
            senderActorRole: currentActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            series: gridAreasWithTransactionId);

        // Act
        await WhenAggregatedMeasureDataProcessesAreInitialized(senderSpy.MessagesSent.ToArray());

        // Assert
        var messages = await ThenAggregatedTimeSeriesRequestServiceBusMessagesAreCorrect(
            senderSpy: senderSpy,
            new List<AggregatedTimeSeriesMessageAssertionInput>
            {
                new(
                    GridAreas: new List<string> { "143" },
                    RequestedForActorNumber: currentActor.ActorNumber.Value,
                    RequestedForActorRole: currentActor.ActorRole.Name,
                    EnergySupplier: energySupplierNumber.Value,
                    BalanceResponsibleParty: balanceResponsibleParty.Value,
                    BusinessReason: BusinessReason.BalanceFixing,
                    Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption),
                new(
                    GridAreas: new List<string> { "512" },
                    RequestedForActorNumber: currentActor.ActorNumber.Value,
                    RequestedForActorRole: currentActor.ActorRole.Name,
                    EnergySupplier: energySupplierNumber.Value,
                    BalanceResponsibleParty: balanceResponsibleParty.Value,
                    BusinessReason: BusinessReason.BalanceFixing,
                    Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption),
                new(
                    GridAreas: new List<string> { "877" },
                    RequestedForActorNumber: currentActor.ActorNumber.Value,
                    RequestedForActorRole: currentActor.ActorRole.Name,
                    EnergySupplier: energySupplierNumber.Value,
                    BalanceResponsibleParty: balanceResponsibleParty.Value,
                    BusinessReason: BusinessReason.BalanceFixing,
                    Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption),
            });

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock AggregatedTimeSeriesRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var acceptedResponses = messages
            .Select(message =>
                (
                    ProcessId: message.ProcessId,
                    AcceptedResponse: AggregatedTimeSeriesResponseEventBuilder
                        .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow())))
            .ToList();

        foreach (var response in acceptedResponses)
        {
            await GivenAggregatedMeasureDataRequestAcceptedIsReceived(response.ProcessId, response.AcceptedResponse);
        }

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            currentActor.ActorNumber,
            currentActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            peekResults
                .Should()
                .HaveCount(3, "because there should be three messages when requesting three series");
        }

        var gridAreas = new List<string>();
        foreach (var peekResult in peekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

            var expectedGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle, peekDocumentFormat);
            gridAreas.Add(expectedGridArea);

            var acceptedResponse = acceptedResponses
                .Should()
                .ContainSingle(r => r.AcceptedResponse.Series.Single().GridArea == expectedGridArea)
                .Subject;

            var expectedTransactionId = gridAreasWithTransactionId
                .Single(ga => ga.GridArea == expectedGridArea)
                .TransactionId;

            await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyAggregatedMeasureDataDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.BalanceFixing,
                        null),
                    ReceiverId: currentActor.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: expectedGridArea,
                    OriginalTransactionIdReference: expectedTransactionId,
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: TimeSeriesPointAssertionInput.From(acceptedResponse.AcceptedResponse.Series.Single().TimeSeriesPoints)));
        }

        gridAreas.Should().BeEquivalentTo("143", "512", "877");
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_NoDataInGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneRejectNotifyAggregatedMeasureDataDocumentWithCorrectContent(
            ActorRole actorRole,
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var testMessageData = actorRole == ActorRole.EnergySupplier
             ? testDataDescription.ExampleEnergySupplier
             : testDataDescription.ExampleBalanceResponsible;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = testMessageData.ExampleMessageData.BalanceResponsible;
        var actor = (ActorNumber: testMessageData.ActorNumber, ActorRole: actorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var gridAreaWithNoData = "000";

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            meteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
            settlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
            periodStart: (2022, 1, 1),
            periodEnd: (2022, 2, 1),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaWithNoData, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber!.Value,
                balanceResponsibleParty!.Value,
                new List<string> { gridAreaWithNoData },
                SettlementMethod: testMessageData.ExampleMessageData.SettlementMethod,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Ingen data tilgængelig / No data available";
        var expectedErrorCode = "E0H";

        // Generate a mock ServiceBus Message with RequestCalculatedEnergyTimeSeriesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedEnergyTimeSeriesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedEnergyTimeSeriesInput = message.ParseInput<RequestCalculatedEnergyTimeSeriesInputV1>();
        var requestCalculatedEnergyTimeSeriesRejected = AggregatedTimeSeriesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedEnergyTimeSeriesInput, expectedErrorMessage, expectedErrorCode);

        await GivenAggregatedMeasureDataRequestRejectedIsReceived(
            requestCalculatedEnergyTimeSeriesRejected);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle("because there should be one message when requesting for one grid area")
                .Subject;
        }

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new(
                BusinessReason.BalanceFixing,
                "5790001330552",
                actor.ActorNumber.Value,
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                ReasonCode.FullyRejected.Code,
                transactionId,
                expectedErrorCode,
                expectedErrorMessage));
    }

    public async Task InitializeAsync()
    {
        await _fixture.InsertAggregatedMeasureDataDatabricksDataAsync(_ediDatabricksOptions);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
