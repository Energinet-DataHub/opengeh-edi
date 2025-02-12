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
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
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

        var expectedGridArea = exampleWholesaleResultMessageForActor.GridArea;
        var expectedChargeCode = exampleWholesaleResultMessageForActor.ChargeCode;
        var expectedChargeType = exampleWholesaleResultMessageForActor.ChargeType!;
        var expectedQuantityMeasurementUnit = exampleWholesaleResultMessageForActor.MeasurementUnit!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        await GivenGridAreaOwnershipAsync(expectedGridArea, gridOperatorNumber);

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: expectedChargeCode,
            chargeType: expectedChargeType,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (expectedGridArea, transactionId),
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
                new List<string> { expectedGridArea },
                null,
                new List<ChargeTypeInput> { new(expectedChargeType.Name, expectedChargeCode) }));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
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
                SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: expectedChargeCode,
                ChargeType: expectedChargeType,
                Currency: exampleWholesaleResultMessageForActor.Currency,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: exampleWholesaleResultMessageForActor.SettlementMethod,
                MeteringPointType: exampleWholesaleResultMessageForActor.MeteringPointType,
                GridArea: expectedGridArea,
                transactionId,
                PriceMeasurementUnit: expectedQuantityMeasurementUnit,
                ProductCode: "5790001330590",
                QuantityMeasurementUnit: expectedQuantityMeasurementUnit,
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

        var expectedChargeCode = exampleWholesaleResultMessageForActor.First().Value.ChargeCode;
        var expectedChargeType = exampleWholesaleResultMessageForActor.First().Value.ChargeType!;
        var expectedQuantityMeasurementUnit = exampleWholesaleResultMessageForActor.First().Value.MeasurementUnit!;

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
            chargeCode: expectedChargeCode,
            chargeType: expectedChargeType,
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
                new List<ChargeTypeInput> { new(expectedChargeType.Name, expectedChargeCode) }));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
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
                    SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerNumber.Value,
                    ChargeCode: expectedChargeCode,
                    ChargeType: expectedChargeType,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierNumber.Value,
                    SettlementMethod: exampleWholesaleResultMessageForActor[seriesRequestGridArea].SettlementMethod,
                    MeteringPointType: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeteringPointType,
                    GridArea: seriesRequestGridArea,
                    transactionId,
                    PriceMeasurementUnit: expectedQuantityMeasurementUnit,
                    ProductCode: "5790001330590",
                    QuantityMeasurementUnit: expectedQuantityMeasurementUnit,
                    CalculationVersion: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Version,
                    Resolution: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Resolution,
                    Period: testDataDescription.Period,
                    Points: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Points));
        }

        resultGridAreas.Should().BeEquivalentTo(acceptedGridAreas);
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_RequestHasNoDataInOptionalFields_When_ActorPeeksAllMessages_Then_ReceivesNotifyWholesaleServicesDocumentWithCorrectContent(
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

        var energySupplierOrNull = actorRole == ActorRole.EnergySupplier
            ? ActorNumber.Create("5790001662233")
            : null;
        var chargeOwnerOrNull = actorRole == ActorRole.SystemOperator
            ? ActorNumber.Create("5790000432752")
            : null;
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var actor = (ActorNumber: energySupplierOrNull != null
            ? energySupplierOrNull
            : chargeOwnerOrNull != null
                ? chargeOwnerOrNull
                : gridOperatorNumber, ActorRole: actorRole);
        var gridAreaOrNull = actorRole == ActorRole.GridAccessProvider
            ? exampleWholesaleResultMessageForActor.GridArea
            : null;
        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierOrNull,
            chargeOwner: chargeOwnerOrNull,
            chargeCode: null,
            chargeType: null,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaOrNull, transactionId),
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
                energySupplierOrNull?.Value,
                chargeOwnerOrNull?.Value,
                GridAreas: gridAreaOrNull != null ? new[] { gridAreaOrNull } : new List<string>(),
                ChargeTypes: null,
                SettlementVersion: null));

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var defaultGridAreas = gridAreaOrNull == null ? testDataDescription.GridAreaCodes : null;
        var defaultChargeOwner = chargeOwnerOrNull == null ? "5790000432752" : null;
        var defaultEnergySupplier = energySupplierOrNull == null ? "5790001662233" : null;
        var requestCalculatedWholesaleServicesInputV1 = message.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesAccepted = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedWholesaleServicesInputV1, gridAreas: defaultGridAreas, defaultChargeOwnerId: defaultChargeOwner, defaultEnergySupplierId: defaultEnergySupplier);

        await GivenWholesaleServicesRequestAcceptedIsReceived(requestCalculatedWholesaleServicesAccepted);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
            peekDocumentFormat);

        // Assert
        var acceptedChargeCodes = new List<string>() { "40000", "41000", "45013", "EA-001" };
        var acceptedResolutions = new List<Resolution>() { Resolution.Daily, Resolution.Hourly };
        using (new AssertionScope())
        {
            peekResults.Should().HaveSameCount(acceptedChargeCodes, "because there should be one message for each charge code");
        }

        foreach (var peekResult in peekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultChargeCode = await GetChargeCodeFromNotifyWholesaleServicesDocument(peekResult.Bundle, peekDocumentFormat);
            var peekResultResolution = await GetResolutionFromNotifyWholesaleServicesDocument(peekResult.Bundle, peekDocumentFormat);

            acceptedChargeCodes
                .Should()
                .ContainSingle(chargeCodes => chargeCodes == peekResultChargeCode);

            var resolution =
                acceptedResolutions
                    .Should()
                    .ContainSingle(resolution => resolution.Code == peekResultResolution)
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
                    SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerOrNull?.Value ?? "5790000432752",
                    ChargeCode: peekResultChargeCode,
                    ChargeType: ChargeType.Tariff,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierOrNull?.Value ?? "5790001662233",
                    SettlementMethod: exampleWholesaleResultMessageForActor.SettlementMethod,
                    MeteringPointType: exampleWholesaleResultMessageForActor.MeteringPointType,
                    GridArea: exampleWholesaleResultMessageForActor.GridArea,
                    transactionId,
                    PriceMeasurementUnit: MeasurementUnit.KilowattHour,
                    ProductCode: "5790001330590",
                    QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                    CalculationVersion: exampleWholesaleResultMessageForActor.Version,
                    Resolution: resolution,
                    Period: testDataDescription.Period,
                    Points: null)); // Points is skipped in this test due to multiple peek results
        }
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
                DataHubDetails.DataHubActorNumber.Value,
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
    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_ResolutionIsMonthlyAndDataInOneGridAreaAndNoChargeTypes_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(
            ActorRole actorRole,
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescriptionForMonthlyAmount = GivenDatabricksResultDataForWholesaleResultMonthlyAmountPerCharge();
        var exampleMonthlyAmountWholesaleResultMessageForActor = actorRole == ActorRole.SystemOperator
            ? testDataDescriptionForMonthlyAmount.ExampleWholesaleResultMessageDataForSystemOperator
            : testDataDescriptionForMonthlyAmount.ExampleWholesaleResultMessageDataForEnergySupplierAndGridOperator;

        var testDataDescriptionForTotalAmount = GivenDatabricksResultDataForWholesaleResultTotalAmount();
        var exampleTotalAmountWholesaleResultMessageForActor = actorRole == ActorRole.SystemOperator
            ? testDataDescriptionForTotalAmount.ExampleWholesaleResultMessageDataForSystemOperator
            : actorRole == ActorRole.EnergySupplier
                ? testDataDescriptionForTotalAmount.ExampleWholesaleResultMessageDataForEnergySupplier
                : testDataDescriptionForTotalAmount.ExampleWholesaleResultMessageDataForChargeOwner;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = actorRole == ActorRole.SystemOperator
                                ? ActorNumber.Create("5790000432752")
                                : actorRole == ActorRole.GridAccessProvider
                                    ? ActorNumber.Create("8500000000502") : null;
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var actor = (ActorNumber: actorRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : chargeOwnerNumber != null
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: actorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var gridArea = testDataDescriptionForMonthlyAmount.GridAreaCodes.Single();

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber!, actor.ActorRole);
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
            chargeCode: null,
            // when chargeType is null, all charge types are requested + the total amount if resolution is monthly
            chargeType: null,
            isMonthly: true,
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
                Resolution: Resolution.Monthly,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber?.Value,
                new List<string> { gridArea },
                null,
                new List<ChargeTypeInput>()));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
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
        var expectedResultsNumber = actorRole == ActorRole.EnergySupplier
                ? 8 // 7 for each charge type (monthly) and 1 for charge owner (total amount)
                : actorRole == ActorRole.GridAccessProvider
                    ? 4 // 3 for each charge type (monthly) and 1 for charge owner (total amount)
                    : 5; // 3 for each charge type (monthly) and 2 for charge owner (total amount)
        using (new AssertionScope())
        {
           peekResults
                .Should()
                .HaveCount(
                    expectedResultsNumber,
                    "because there should be one message when requesting for one grid area per the monthly charge type summary and one total amount");
        }

        var expectedChargeType = actorRole == ActorRole.SystemOperator
            ? ChargeType.Tariff
            : ChargeType.Subscription;

        var expectedChargeCode = actorRole == ActorRole.SystemOperator
            ? "45013"
            : "Sub-804";

        var monthlyAmountPeekResult = await GetMonthlyAmountPeekResult(peekResults, peekDocumentFormat, expectedChargeType, expectedChargeCode);
        monthlyAmountPeekResult.Should().NotBeNull();
        var monthlyAmountWholesaleServicesDocumentAssertionInput = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2024-07-01T14:57:09Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.WholesaleFixing,
                null),
            ReceiverId: actor.ActorNumber.Value,
            ReceiverRole: actor.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: actorRole == ActorRole.EnergySupplier ? "8500000000502" : chargeOwnerNumber!.Value,
            ChargeCode: expectedChargeCode,
            ChargeType: expectedChargeType,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: energySupplierNumber.Value,
            SettlementMethod: exampleMonthlyAmountWholesaleResultMessageForActor.SettlementMethod,
            MeteringPointType: exampleMonthlyAmountWholesaleResultMessageForActor.MeteringPointType,
            GridArea: gridArea,
            TransactionId.From("12356478912356478912356478912356478"),
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590",
            QuantityMeasurementUnit: exampleMonthlyAmountWholesaleResultMessageForActor.MeasurementUnit!,
            CalculationVersion: exampleMonthlyAmountWholesaleResultMessageForActor.Version,
            Resolution: exampleMonthlyAmountWholesaleResultMessageForActor.Resolution,
            Period: testDataDescriptionForMonthlyAmount.Period,
            Points: exampleMonthlyAmountWholesaleResultMessageForActor.Points);

        var totalMonthlyAmountPeekResult = await GetTotalAmountPeekResult(peekResults, peekDocumentFormat);
        var totalMonthlyAmountWholesaleServicesDocumentAssertionInput = new NotifyWholesaleServicesDocumentAssertionInput(
            Timestamp: "2024-07-01T14:57:09Z",
            BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                BusinessReason.WholesaleFixing,
                null),
            ReceiverId: actor.ActorNumber.Value,
            ReceiverRole: actor.ActorRole,
            SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
            SenderRole: ActorRole.MeteredDataAdministrator,
            ChargeTypeOwner: null,
            ChargeCode: exampleTotalAmountWholesaleResultMessageForActor.ChargeCode,
            ChargeType: exampleTotalAmountWholesaleResultMessageForActor.ChargeType,
            Currency: Currency.DanishCrowns,
            EnergySupplierNumber: energySupplierNumber.Value,
            SettlementMethod: exampleTotalAmountWholesaleResultMessageForActor.SettlementMethod,
            MeteringPointType: exampleTotalAmountWholesaleResultMessageForActor.MeteringPointType,
            GridArea: gridArea,
            TransactionId.From("12356478912356478912356478912356478"),
            PriceMeasurementUnit: null,
            ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
            CalculationVersion: exampleTotalAmountWholesaleResultMessageForActor.Version,
            Resolution: exampleTotalAmountWholesaleResultMessageForActor.Resolution,
            Period: testDataDescriptionForMonthlyAmount.Period,
            Points: exampleTotalAmountWholesaleResultMessageForActor.Points);

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            monthlyAmountPeekResult!.Bundle,
            peekDocumentFormat,
            monthlyAmountWholesaleServicesDocumentAssertionInput);

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            totalMonthlyAmountPeekResult!.Bundle,
            peekDocumentFormat,
            totalMonthlyAmountWholesaleServicesDocumentAssertionInput);
    }

    private async Task<PeekResultDto?> GetMonthlyAmountPeekResult(
        List<PeekResultDto> peekResults,
        DocumentFormat peekDocumentFormat,
        ChargeType chargeType,
        string chargeCode)
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
                && stringContent.Contains($"<cim:chargeType.type>{chargeType.Code}</cim:chargeType.type>", StringComparison.InvariantCultureIgnoreCase)
                && stringContent.Contains($"<cim:chargeType.mRID>{chargeCode}</cim:chargeType.mRID>", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is CIM json and contains the charge type, it is the monthly amount document
            var valueChargeType = $"\"chargeType.type\": {{\r\n          \"value\": \"{chargeType.Code}\"\r\n        }}";
            var valueChargeCode = $"\"chargeType.mRID\": \"{chargeCode}\"";
            if (peekDocumentFormat == DocumentFormat.Json
                && stringContent.Contains(valueChargeType, StringComparison.InvariantCultureIgnoreCase)
                && stringContent.Contains(valueChargeCode, StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is Ebix and contains the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Ebix
                && stringContent.Contains($"<ns0:ChargeType listAgencyIdentifier=\"260\" listIdentifier=\"DK\">{chargeType.Code}</ns0:ChargeType>", StringComparison.InvariantCultureIgnoreCase)
                && stringContent.Contains($"<ns0:PartyChargeTypeID>{chargeCode}</ns0:PartyChargeTypeID>", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }
        }

        return null;
    }

    private async Task<PeekResultDto?> GetTotalAmountPeekResult(List<PeekResultDto> peekResults, DocumentFormat peekDocumentFormat)
    {
        foreach (var peekResult in peekResults)
        {
            // We cannot dispose the stream reader, disposing a stream reader disposes the underlying stream, which means we can't read it again
            var streamReader = new StreamReader(peekResult.Bundle, Encoding.UTF8);
            var stringContent = await streamReader.ReadToEndAsync();
            peekResult.Bundle.Position = 0;
            streamReader.DiscardBufferedData();
            // If the document is CIM xml and does not contain the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Xml
                && !stringContent.Contains("<cim:chargeType.type>", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is CIM json and  does not contain the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Json
                && !stringContent.Contains("chargeType.type", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }

            // If the document is Ebix and  does not contain the charge type, it is the monthly amount document
            if (peekDocumentFormat == DocumentFormat.Ebix
                && !stringContent.Contains("ns0:ChargeType listAgencyIdentifier", StringComparison.InvariantCultureIgnoreCase))
            {
                return peekResult;
            }
        }

        return null;
    }
}
