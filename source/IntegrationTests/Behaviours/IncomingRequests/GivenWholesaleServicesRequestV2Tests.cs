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
using System.Text;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenWholesaleServicesRequestV2Tests : WholesaleServicesBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenWholesaleServicesRequestV2Tests(IntegrationTestFixture fixture, ITestOutputHelper testOutput)
        : base(fixture, testOutput)
    {
        _fixture = fixture;
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration, true);
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public static object[][] DocumentFormatsWithActorRoleCombinationsForNullGridArea() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: true);

    public static object[][] DocumentFormatsWithAllActorRoleCombinations() =>
        DocumentFormatsWithActorRoleCombinations(nullGridArea: false);

    public static object[][] DocumentFormatsWithActorRoleCombinations(bool nullGridArea)
    {
        // The actor roles who can perform WholesaleServicesRequest's
        var actorRoles = new List<ActorRole> { ActorRole.EnergySupplier, ActorRole.SystemOperator, };

        if (!nullGridArea)
        {
            actorRoles.Add(ActorRole.GridAccessProvider);
        }

        var incomingDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(
                except: new[]
                {
                    DocumentFormat.Ebix.Name, // ebIX is not supported for requests
                })
            .ToArray();

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return actorRoles
            .SelectMany(
                actorRole => incomingDocumentFormats
                    .SelectMany(
                        incomingDocumentFormat => peekDocumentFormats
                            .Select(
                                peekDocumentFormat =>
                                    new object[] { actorRole, incomingDocumentFormat, peekDocumentFormat, })))
            .ToArray();
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
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(
            ActorRole actorRole,
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var exampleWholesaleResultMessageForActor = actorRole == ActorRole.SystemOperator
            ? testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator
            : testDataDescription.ExampleWholesaleResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = actorRole == ActorRole.SystemOperator ? ActorNumber.Create("5790000432752") : ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var actor = (ActorNumber: actorRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : actorRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: actorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var gridArea = exampleWholesaleResultMessageForActor.GridArea;
        var chargeCode = exampleWholesaleResultMessageForActor.ChargeCode;
        var chargeType = exampleWholesaleResultMessageForActor.ChargeType!;
        var quantityMeasurementUnit = exampleWholesaleResultMessageForActor.MeasurementUnit!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        await GivenGridAreaOwnershipAsync(gridArea, gridOperatorNumber);

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: chargeCode,
            chargeType: chargeType,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridArea, transactionId),
            });

        // Assert
        var message = await ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                new List<string> { gridArea },
                null,
                new List<ChargeTypeInput> { new(chargeType.Name, chargeCode) }));

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedWholesaleServicesInputV1 = message.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesAccepted = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedWholesaleServicesInputV1);

        await GivenWholesaleServicesRequestAcceptedIsReceived(requestCalculatedWholesaleServicesAccepted);

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

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.WholesaleFixing,
                    null),
                ReceiverId: actor.ActorNumber.Value,
                ReceiverRole: actor.ActorRole,
                SenderId: "5790001330552", // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: chargeCode,
                ChargeType: chargeType,
                Currency: exampleWholesaleResultMessageForActor.Currency,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: exampleWholesaleResultMessageForActor.SettlementMethod,
                MeteringPointType: exampleWholesaleResultMessageForActor.MeteringPointType,
                GridArea: gridArea,
                transactionId,
                PriceMeasurementUnit: quantityMeasurementUnit,
                ProductCode: "5790001330590",
                QuantityMeasurementUnit: quantityMeasurementUnit,
                CalculationVersion: exampleWholesaleResultMessageForActor.Version,
                Resolution: exampleWholesaleResultMessageForActor.Resolution,
                Period: testDataDescription.Period,
                Points: exampleWholesaleResultMessageForActor.Points));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinationsForNullGridArea))]
    public async Task AndGiven_DataInTwoGridAreas_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(
        ActorRole actorRole,
        DocumentFormat incomingDocumentFormat,
        DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreas();
        var exampleWholesaleResultMessageForActor = actorRole == ActorRole.EnergySupplier
            ? testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier
            : testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = actorRole == ActorRole.SystemOperator ? ActorNumber.Create("5790000432752") : ActorNumber.Create("8500000000502");
        var actor = (ActorNumber: actorRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : chargeOwnerNumber, ActorRole: actorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var chargeCode = exampleWholesaleResultMessageForActor.First().Value.ChargeCode;
        var chargeType = exampleWholesaleResultMessageForActor.First().Value.ChargeType!;
        var quantityMeasurementUnit = exampleWholesaleResultMessageForActor.First().Value.MeasurementUnit!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);

        foreach (var gridAreaOwner in testDataDescription.GridAreaOwners)
        {
            await GivenGridAreaOwnershipAsync(gridAreaOwner.Key, gridAreaOwner.Value);
        }

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: chargeCode,
            chargeType: chargeType,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, transactionId),
            });

        // Assert
        var message = await ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                GridAreas: new List<string>
                {
                    Capacity = 0,
                },
                null,
                new List<ChargeTypeInput> { new(chargeType.Name, chargeCode) }));

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesInputV1 for each grid area
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var acceptedGridAreas = testDataDescription.GridAreaCodes.ToList().AsReadOnly();
        var requestCalculatedWholesaleServicesInputV1 = message.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesAccepted = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedWholesaleServicesInputV1, acceptedGridAreas);

        await GivenWholesaleServicesRequestAcceptedIsReceived(requestCalculatedWholesaleServicesAccepted);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            peekResults.Should().HaveSameCount(acceptedGridAreas, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in peekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            var seriesRequestGridArea = acceptedGridAreas
                .Should().ContainSingle(gridArea => gridArea == peekResultGridArea)
                .Subject;

            await ThenNotifyWholesaleServicesDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyWholesaleServicesDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.WholesaleFixing,
                        null),
                    ReceiverId: actor.ActorNumber.Value,
                    ReceiverRole: actor.ActorRole,
                    SenderId: "5790001330552", // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerNumber.Value,
                    ChargeCode: chargeCode,
                    ChargeType: chargeType,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierNumber.Value,
                    SettlementMethod: exampleWholesaleResultMessageForActor[seriesRequestGridArea].SettlementMethod,
                    MeteringPointType: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeteringPointType,
                    GridArea: seriesRequestGridArea,
                    transactionId,
                    PriceMeasurementUnit: quantityMeasurementUnit,
                    ProductCode: "5790001330590",
                    QuantityMeasurementUnit: quantityMeasurementUnit,
                    CalculationVersion: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Version,
                    Resolution: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Resolution,
                    Period: testDataDescription.Period,
                    Points: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Points));
        }

        resultGridAreas.Should().BeEquivalentTo(acceptedGridAreas);
    }

    [Theory(Skip = "not updated")]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_RequestHasNoDataInOptionalFields_When_ActorPeeksAllMessages_Then_ReceivesNotifyWholesaleServicesDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierOrNull = actor.ActorRole == ActorRole.EnergySupplier
            ? actor.ActorNumber
            : null;
        var chargeOwnerOrNull = actor.ActorRole == ActorRole.SystemOperator
            ? actor.ActorNumber
            : null;
        var gridAreaOrNull = actor.ActorRole == ActorRole.GridAccessProvider
            ? "512"
            : null;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierOrNull,
            chargeOwner: chargeOwnerOrNull,
            chargeCode: null,
            chargeType: null,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaOrNull, TransactionId.From("12356478912356478912356478912356478")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: gridAreaOrNull != null ? new[] { gridAreaOrNull } : new List<string>(),
                RequestedForActorNumber: actor.ActorNumber.Value,
                RequestedForActorRole: actor.ActorRole.Name,
                EnergySupplierId: energySupplierOrNull?.Value,
                ChargeOwnerId: chargeOwnerOrNull?.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: null,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleServicesRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = gridAreaOrNull == null ? new List<string> { "512" } : null;
        var defaultChargeOwner = chargeOwnerOrNull == null ? "5799999933444" : null;
        var defaultEnergySupplier = energySupplierOrNull == null ? "5790001330552" : null;
        var acceptedResponse = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow(), defaultChargeOwner, defaultEnergySupplier, defaultGridAreas);

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, acceptedResponse);

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

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.WholesaleFixing,
                    null),
                ReceiverId: actor.ActorNumber.Value,
                ReceiverRole: actor.ActorRole,
                SenderId: "5790001330552", // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerOrNull?.Value ?? "5799999933444",
                ChargeCode: "12345678",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: energySupplierOrNull?.Value ?? "5790001330552",
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "512",
                TransactionId.From("12356478912356478912356478912356478"),
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: acceptedResponse.Series.Single().TimeSeriesPoints));
    }

    [Theory(Skip = "not updated")]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task AndGiven_RequestedThreeSeries_When_ActorPeeksAllMessages_Then_ReceivesThreeNotifyWholesaleServicesDocumentsWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierNumber = actor.ActorRole == ActorRole.EnergySupplier
            ? actor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = actor.ActorRole == ActorRole.SystemOperator
            ? actor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = actor.ActorRole == ActorRole.GridAccessProvider
            ? actor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        await GivenGridAreaOwnershipAsync("143", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("877", gridOperatorNumber);

        var gridAreasWithTransactionId = new (string? GridArea, TransactionId TransactionId)[]
        {
            ("143", TransactionId.From("12356478912356478912356478912356476")),
            ("512", TransactionId.From("12356478912356478912356478912356477")),
            ("877", TransactionId.From("12356478912356478912356478912356478")),
        };

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            series: gridAreasWithTransactionId);

        // Act
        await WhenWholesaleServicesProcessesAreInitialized(senderSpy.MessagesSent.ToArray());

        // Assert
        var messages = await ThenWholesaleServicesRequestServiceBusMessagesAreCorrect(
            senderSpy: senderSpy,
            new List<WholesaleServicesMessageAssertionInput>
            {
                new(
                    GridAreas: new List<string>() { "143" },
                    RequestedForActorNumber: actor.ActorNumber.Value,
                    RequestedForActorRole: actor.ActorRole.Name,
                    EnergySupplierId: energySupplierNumber.Value,
                    ChargeOwnerId: chargeOwnerNumber.Value,
                    Resolution: null,
                    BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (DataHubNames.ChargeType.Tariff, "25361478"),
                    },
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null),
                new(
                    GridAreas: new List<string>() { "512" },
                    RequestedForActorNumber: actor.ActorNumber.Value,
                    RequestedForActorRole: actor.ActorRole.Name,
                    EnergySupplierId: energySupplierNumber.Value,
                    ChargeOwnerId: chargeOwnerNumber.Value,
                    Resolution: null,
                    BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (DataHubNames.ChargeType.Tariff, "25361478"),
                    },
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null),
                new(
                    GridAreas: new List<string>() { "877" },
                    RequestedForActorNumber: actor.ActorNumber.Value,
                    RequestedForActorRole: actor.ActorRole.Name,
                    EnergySupplierId: energySupplierNumber.Value,
                    ChargeOwnerId: chargeOwnerNumber.Value,
                    Resolution: null,
                    BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (DataHubNames.ChargeType.Tariff, "25361478"),
                    },
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    SettlementVersion: null),
            });

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleServicesRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var acceptedResponses = messages
            .Select(message =>
                (
                    message.ProcessId,
                    AcceptedResponse: WholesaleServicesResponseEventBuilder
                        .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow())))
            .ToList();

        foreach (var response in acceptedResponses)
        {
            await GivenWholesaleServicesRequestAcceptedIsReceived(response.ProcessId, response.AcceptedResponse);
        }

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
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

            var expectedGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle, peekDocumentFormat);
            gridAreas.Add(expectedGridArea);

            var acceptedResponse = acceptedResponses
                .Should()
                .ContainSingle(r => r.AcceptedResponse.Series.Single().GridArea == expectedGridArea)
                .Subject;

            var seriesRequest = acceptedResponse.AcceptedResponse.Series
                .Should().ContainSingle(request => request.GridArea == expectedGridArea)
                .Subject;

            var expectedTransactionId = gridAreasWithTransactionId
                .Single(ga => ga.GridArea == expectedGridArea)
                .TransactionId;

            await ThenNotifyWholesaleServicesDocumentIsCorrect(
                peekResult.Bundle,
                peekDocumentFormat,
                new NotifyWholesaleServicesDocumentAssertionInput(
                    Timestamp: "2024-07-01T14:57:09Z",
                    BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                        BusinessReason.WholesaleFixing,
                        null),
                    ReceiverId: actor.ActorNumber.Value,
                    ReceiverRole: actor.ActorRole,
                    SenderId: "5790001330552", // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerNumber.Value,
                    ChargeCode: "25361478",
                    ChargeType: ChargeType.Tariff,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierNumber.Value,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridArea: seriesRequest.GridArea,
                    OriginalTransactionIdReference: expectedTransactionId,
                    PriceMeasurementUnit: MeasurementUnit.Kwh,
                    ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: seriesRequest.TimeSeriesPoints));
        }

        gridAreas.Should().BeEquivalentTo("143", "512", "877");
    }

    [Theory]
    [InlineData("Xml")]
    [InlineData("Json")]
    public async Task AndGiven_RequestContainsWrongEnergySupplierInSeries_When_OriginalEnergySupplier_Then_ReceivesOneRejectWholesaleSettlementDocumentsWithCorrectContent(string incomingDocumentFormatName)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var incomingDocumentFormat = DocumentFormat.FromName(incomingDocumentFormatName);
        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var energySupplierNumber = ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = ActorNumber.Create("5799999933444");
        var gridArea = "512";
        var transactionId = TransactionId.From("123564789123564789123564789123564787");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridArea, transactionId),
            });

        // Assert
        var message = await ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2024, 1, 1),
                PeriodEnd: CreateDateInstant(2024, 1, 31),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                new List<string> { gridArea },
                SettlementVersion: null,
                new List<ChargeTypeInput> { new(DataHubNames.ChargeType.Tariff, "25361478") }));

        /*
         *  --- PART 2: Receive reject response from Process Manager and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Elleverandør i header og payload stemmer ikke overens / "
                                    + "Energysupplier in header and payload must be the same";
        var expectedErrorCode = "E16";

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesRejectedV1 response from Process Manager
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedWholesaleServicesInputV1 = message.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesRejected = WholesaleServicesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedWholesaleServicesInputV1, expectedErrorMessage, expectedErrorCode);

        await GivenWholesaleServicesRequestRejectedIsReceived(requestCalculatedWholesaleServicesRejected);

        // Act
        var actorPeekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            incomingDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = actorPeekResults.Should().ContainSingle("because there should only be one rejected message.")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
            peekResult.Bundle,
            incomingDocumentFormat,
            new RejectRequestWholesaleSettlementDocumentAssertionInput(
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                BusinessReason.WholesaleFixing,
                actor.ActorNumber.Value,
                actor.ActorRole,
                "5790001330552",
                ActorRole.MeteredDataAdministrator,
                ReasonCode.FullyRejected.Code,
                transactionId,
                expectedErrorCode,
                expectedErrorMessage));
    }

    /// <summary>
    /// Request All monthly amounts including total monthly amount
    /// </summary>
    /// <param name="actorRole"></param>
    /// <param name="incomingDocumentFormat"></param>
    /// <param name="peekDocumentFormat"></param>
    [Theory(Skip = "not updated")]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_ResolutionIsMonthlyAndDataInOneGridAreaAndNoChargeTypes_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(
            ActorRole actorRole,
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierNumber = actor.ActorRole == ActorRole.EnergySupplier
            ? actor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = actor.ActorRole == ActorRole.SystemOperator
            ? actor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = actor.ActorRole == ActorRole.GridAccessProvider
            ? actor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: null,
            // when chargeType is null, all charge types are requested + the total amount if resolution is monthly
            chargeType: null,
            isMonthly: true,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("12356478912356478912356478912356478")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                new List<string> { "512" },
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                Resolution.Monthly.Name,
                DataHubNames.BusinessReason.WholesaleFixing,
                null,
                new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                null));

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleServicesRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var acceptedResponse = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, acceptedResponse);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
           peekResults
                .Should()
                .HaveCount(
                    2,
                    "because there should be one message when requesting for one grid area per the monthly charge type summary and one total amount");
        }

        var monthlyAmountPeekResult = await GetMonthlyAmountPeekResult(peekResults, peekDocumentFormat);
        monthlyAmountPeekResult.Should().NotBeNull();
        var monthlyAmountWholesaleServicesDocumentAssertionInput = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2024-07-01T14:57:09Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.WholesaleFixing,
                null),
            ReceiverId: actor.ActorNumber.Value,
            ReceiverRole: actor.ActorRole,
            SenderId: "5790001330552", // Sender is always DataHub
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: chargeOwnerNumber.Value,
            ChargeCode: "12345678",
            ChargeType: ChargeType.Tariff,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: energySupplierNumber.Value,
            SettlementMethod: SettlementMethod.Flex,
            MeteringPointType: MeteringPointType.Consumption,
            GridArea: "512",
            TransactionId.From("12356478912356478912356478912356478"),
            PriceMeasurementUnit: MeasurementUnit.Kwh,
            ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: GetNow().ToUnixTimeTicks(),
            Resolution: Resolution.Monthly,
            Period: new Period(
                CreateDateInstant(2024, 1, 1),
                CreateDateInstant(2024, 1, 31)),
            Points: acceptedResponse.Series.Single(x => x.ChargeType != WholesaleServicesRequestSeries.Types.ChargeType.Unspecified).TimeSeriesPoints);

        var totalMonthlyAmountPeekResult = peekResults.Single(r => r.MessageId != monthlyAmountPeekResult!.MessageId);
        var totalMonthlyAmountWholesaleServicesDocumentAssertionInput = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2024-07-01T14:57:09Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.WholesaleFixing,
                null),
            ReceiverId: actor.ActorNumber.Value,
            ReceiverRole: actor.ActorRole,
            SenderId: "5790001330552", // Sender is always DataHub
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: null,
            ChargeType: null,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: energySupplierNumber.Value,
            SettlementMethod: null,
            MeteringPointType: null,
            GridArea: "512",
            TransactionId.From("12356478912356478912356478912356478"),
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
            QuantityMeasurementUnit: MeasurementUnit.Kwh,
            CalculationVersion: GetNow().ToUnixTimeTicks(),
            Resolution: Resolution.Monthly,
            Period: new Period(
                CreateDateInstant(2024, 1, 1),
                CreateDateInstant(2024, 1, 31)),
            Points: acceptedResponse.Series.Single(x => x.ChargeType == WholesaleServicesRequestSeries.Types.ChargeType.Unspecified).TimeSeriesPoints);

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            monthlyAmountPeekResult!.Bundle,
            peekDocumentFormat,
            monthlyAmountWholesaleServicesDocumentAssertionInput);

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            totalMonthlyAmountPeekResult.Bundle,
            peekDocumentFormat,
            totalMonthlyAmountWholesaleServicesDocumentAssertionInput);
    }

    private async Task<PeekResultDto?> GetMonthlyAmountPeekResult(List<PeekResultDto> peekResults, DocumentFormat peekDocumentFormat)
    {
        foreach (var peekResult in peekResults)
        {
            // We cannot dispose the stream reader, disposing a stream reader disposes the underlying stream, which means we can't read it again
            var streamReader = new StreamReader(peekResult.Bundle, Encoding.UTF8);
            var stringContent = await streamReader.ReadToEndAsync();
            peekResult.Bundle.Position = 0;
            streamReader.DiscardBufferedData();
            // If the document is CIM xml and contains the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Xml
                && stringContent.Contains("<cim:chargeType.type>", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is CIM json and contains the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Json
                && stringContent.Contains("chargeType.type", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is Ebix and contains the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Ebix
                && stringContent.Contains("ns0:ChargeType listAgencyIdentifier", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }
        }

        return null;
    }
}
