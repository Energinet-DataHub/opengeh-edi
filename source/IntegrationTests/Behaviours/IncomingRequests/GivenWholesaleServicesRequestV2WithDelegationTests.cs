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
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenWholesaleServicesRequestV2WithDelegationTests : WholesaleServicesBehaviourTestBase, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions;

    public GivenWholesaleServicesRequestV2WithDelegationTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        _fixture = fixture;
        FeatureFlagManagerStub.SetFeatureFlag(FeatureFlagName.UseRequestWholesaleServicesProcessOrchestration, true);
        _ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
    }

    public static object[][] DocumentFormatsWithAllRoleCombinations() => DocumentFormatsWithRoleCombinations(false);

    public static object[][] DocumentFormatsWithRoleCombinationsForNullGridArea() => DocumentFormatsWithRoleCombinations(true);

    public static object[][] DocumentFormatsWithRoleCombinations(bool nullGridArea)
    {
        var roleCombinations = new List<(ActorRole DelegatedFrom, ActorRole DelegatedTo)>
        {
            // Energy supplier and system operator can only delegate to delegated, not to grid operator
            (ActorRole.EnergySupplier, ActorRole.Delegated),
            (ActorRole.SystemOperator, ActorRole.Delegated),
        };

        // Grid operator cannot make requests with null grid area
        if (!nullGridArea)
        {
            // Grid operator can delegate to both delegated and grid operator
            roleCombinations.Add((ActorRole.GridAccessProvider, ActorRole.Delegated));
            roleCombinations.Add((ActorRole.GridAccessProvider, ActorRole.GridAccessProvider));
        }

        var requestDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(except: new[]
            {
                DocumentFormat.Ebix.Name, // ebIX is not supported for requests
            })
            .ToArray();

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return roleCombinations.SelectMany(d => requestDocumentFormats
            .SelectMany(
                incomingDocumentFormat => peekDocumentFormats
                    .Select(
                        peekDocumentFormat => new object[]
                        {
                            incomingDocumentFormat,
                            peekDocumentFormat,
                            d.DelegatedFrom,
                            d.DelegatedTo,
                        })))
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
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task
        AndGiven_DelegationInOneGridArea_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var exampleWholesaleResultMessageForActor = delegatedToRole == ActorRole.SystemOperator
            ? testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator
            : testDataDescription.ExampleWholesaleResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        var gridAreaCode = exampleWholesaleResultMessageForActor.GridArea;
        var expectedChargeCode = exampleWholesaleResultMessageForActor.ChargeCode;
        var expectedChargeType = exampleWholesaleResultMessageForActor.ChargeType!;

        await GivenGridAreaOwnershipAsync(gridAreaCode, gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaCode,
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: expectedChargeCode,
            chargeType: expectedChargeType,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaCode, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                new List<string> { gridAreaCode },
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
        var requestCalculatedWholesaleServicesInputV1 = message!.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesAccepted = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedWholesaleServicesInputV1);

        await GivenWholesaleServicesRequestAcceptedIsReceived(requestCalculatedWholesaleServicesAccepted);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should().ContainSingle("because there should only be one message for one grid area")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.WholesaleFixing,
                    null),
                ReceiverId: delegatedToActor.ActorNumber.Value,
                ReceiverRole: originalActor.ActorRole,
                SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: expectedChargeCode,
                ChargeType: expectedChargeType,
                Currency: exampleWholesaleResultMessageForActor.Currency,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: exampleWholesaleResultMessageForActor.SettlementMethod,
                MeteringPointType: exampleWholesaleResultMessageForActor.MeteringPointType,
                GridArea: gridAreaCode,
                transactionId,
                PriceMeasurementUnit: exampleWholesaleResultMessageForActor.MeasurementUnit,
                ProductCode: "5790001330590",
                QuantityMeasurementUnit: exampleWholesaleResultMessageForActor.MeasurementUnit!,
                CalculationVersion: exampleWholesaleResultMessageForActor.Version,
                Resolution: exampleWholesaleResultMessageForActor.Resolution,
                Period: testDataDescription.Period,
                Points: exampleWholesaleResultMessageForActor.Points));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea))]
    public async Task
        AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentsWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreas();
        var exampleWholesaleResultMessageForActor = delegatedFromRole == ActorRole.EnergySupplier
            ? testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier
            : testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = delegatedFromRole == ActorRole.SystemOperator
            ? ActorNumber.Create("5790000432752")
            : ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var expectedChargeCode = exampleWholesaleResultMessageForActor.First().Value.ChargeCode;
        var expectedChargeType = exampleWholesaleResultMessageForActor.First().Value.ChargeType!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        foreach (var gridAreaOwner in testDataDescription.GridAreaOwners)
        {
            await GivenGridAreaOwnershipAsync(gridAreaOwner.Key, gridAreaOwner.Value);
            await GivenDelegation(
                new(originalActor.ActorNumber, originalActor.ActorRole),
                new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
                gridAreaOwner.Key,
                ProcessType.RequestWholesaleResults,
                GetNow());
        }

        // Act
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
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
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                GridAreas: testDataDescription.GridAreaCodes,
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
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            delegatedActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in delegatedActorPeekResults)
        {
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
                    ReceiverId: delegatedToActor.ActorNumber.Value,
                    ReceiverRole: originalActor.ActorRole,
                    SenderId: DataHubDetails.DataHubActorNumber.Value,  // Sender is always DataHub
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
                    PriceMeasurementUnit: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeasurementUnit,
                    ProductCode: "5790001330590",
                    QuantityMeasurementUnit: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeasurementUnit!,
                    CalculationVersion: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Version,
                    Resolution: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Resolution,
                    Period: testDataDescription.Period,
                    Points: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Points));
        }

        resultGridAreas.Should().BeEquivalentTo("803", "804");
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task
        AndGiven_InvalidRequestWithDelegationInOneGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectWholesaleSettlementDocumentsWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var exampleWholesaleResultMessageForActor = delegatedToRole == ActorRole.SystemOperator
            ? testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator
            : testDataDescription.ExampleWholesaleResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        var gridAreaCode = exampleWholesaleResultMessageForActor.GridArea;
        await GivenGridAreaOwnershipAsync(gridAreaCode, gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaCode,
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Act
        // Setup fake request (period end is before period start)
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 01, 01),
            periodEnd: (2023, 12, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaCode, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2024, 1, 1),
                PeriodEnd: CreateDateInstant(2023, 12, 31),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                new List<string> { gridAreaCode },
                null,
                new List<ChargeTypeInput> { new(DataHubNames.ChargeType.Tariff, "25361478"), }));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse"
                                                               + " med en engrosfiksering eller korrektioner / It is only possible to request"
                                                               + " data for a full month in relation to wholesalefixing or corrections";
        var expectedErrorCode = "E17";

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesRejectedV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedWholesaleServicesInputV1 = message!.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesRejected = WholesaleServicesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedWholesaleServicesInputV1, expectedErrorMessage, expectedErrorCode);

        await GivenWholesaleServicesRequestRejectedIsReceived(requestCalculatedWholesaleServicesRejected);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should().ContainSingle("because there should only be one rejected message regardless of grid areas")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new RejectRequestWholesaleSettlementDocumentAssertionInput(
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                BusinessReason.WholesaleFixing,
                delegatedToActor.ActorNumber.Value,
                originalActor.ActorRole,
                "5790001330552",
                ActorRole.MeteredDataAdministrator,
                ReasonCode.FullyRejected.Code,
                TransactionId.From("12356478912356478912356478912356478"),
                expectedErrorCode,
                expectedErrorMessage));
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea))]
    public async Task AndGiven_InvalidRequestWithDelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectWholesaleSettlementDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreas();
        var exampleWholesaleResultMessageForActor = delegatedToRole == ActorRole.EnergySupplier
            ? testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier
            : testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = ActorNumber.Create("5790000432752");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        foreach (var gridAreaOwner in testDataDescription.GridAreaOwners)
        {
            await GivenGridAreaOwnershipAsync(gridAreaOwner.Key, gridAreaOwner.Value);
            await GivenDelegation(
                new(originalActor.ActorNumber, originalActor.ActorRole),
                new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
                gridAreaOwner.Key,
                ProcessType.RequestWholesaleResults,
                GetNow());
        }

        // Act
        // Setup fake request (period end is before period start)
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 01, 01),
            periodEnd: (2023, 12, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2024, 1, 1),
                PeriodEnd: CreateDateInstant(2023, 12, 31),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                GridAreas: testDataDescription.GridAreaCodes,
                null,
                new List<ChargeTypeInput> { new(DataHubNames.ChargeType.Tariff, "25361478") }));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange
        var expectedErrorMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse"
                                   + " med en engrosfiksering eller korrektioner / It is only possible to request"
                                   + " data for a full month in relation to wholesalefixing or corrections";
        var expectedErrorCode = "E17";

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesRejectedV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedWholesaleServicesInputV1 = message!.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesRejected = WholesaleServicesResponseEventBuilder
            .GenerateRejectedFrom(requestCalculatedWholesaleServicesInputV1, expectedErrorMessage, expectedErrorCode);

        await GivenWholesaleServicesRequestRejectedIsReceived(requestCalculatedWholesaleServicesRejected);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            peekResult = delegatedActorPeekResults.Should().ContainSingle("because there should only be one rejected message regardless of grid areas")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new RejectRequestWholesaleSettlementDocumentAssertionInput(
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                BusinessReason.WholesaleFixing,
                delegatedToActor.ActorNumber.Value,
                originalActor.ActorRole,
                "5790001330552",
                ActorRole.MeteredDataAdministrator,
                ReasonCode.FullyRejected.Code,
                transactionId,
                expectedErrorCode,
                expectedErrorMessage));
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea))] // Grid operator can't make request without grid area
    public async Task
        AndGiven_OriginalActorRequestsOwnDataWithDataInTwoGridAreas_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerChargeInTwoGridAreas();
        var exampleWholesaleResultMessageForActor = delegatedFromRole == ActorRole.EnergySupplier
            ? testDataDescription.ExampleWholesaleResultMessageDataForEnergySupplier
            : testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = delegatedFromRole == ActorRole.SystemOperator
            ? ActorNumber.Create("5790000432752")
            : ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var expectedChargeCode = exampleWholesaleResultMessageForActor.First().Value.ChargeCode;
        var expectedChargeType = exampleWholesaleResultMessageForActor.First().Value.ChargeType!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);
        foreach (var gridAreaOwner in testDataDescription.GridAreaOwners)
        {
            await GivenGridAreaOwnershipAsync(gridAreaOwner.Key, gridAreaOwner.Value);
        }

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            testDataDescription.GridAreaCodes.First(),
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Act
        // Original actor requests own data
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
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
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                GridAreas: Array.Empty<string>(),
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
        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        // Assert
        using (new AssertionScope())
        {
            delegatedActorPeekResults.Should().BeEmpty("because delegated actor shouldn't receive result when original actor made the request");
            originalActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in originalActorPeekResults)
        {
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
                    ReceiverId: originalActor.ActorNumber.Value,
                    ReceiverRole: originalActor.ActorRole,
                    SenderId: DataHubDetails.DataHubActorNumber.Value,  // Sender is always DataHub
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
                    PriceMeasurementUnit: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeasurementUnit,
                    ProductCode: "5790001330590",
                    QuantityMeasurementUnit: exampleWholesaleResultMessageForActor[seriesRequestGridArea].MeasurementUnit!,
                    CalculationVersion: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Version,
                    Resolution: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Resolution,
                    Period: testDataDescription.Period,
                    Points: exampleWholesaleResultMessageForActor[seriesRequestGridArea].Points));
        }

        resultGridAreas.Should().BeEquivalentTo("803", "804");
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task
        AndGiven_OriginalActorRequestsOwnDataWithGridArea_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(
            DocumentFormat incomingDocumentFormat,
            DocumentFormat peekDocumentFormat,
            ActorRole delegatedFromRole,
            ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var exampleWholesaleResultMessageForActor = delegatedToRole == ActorRole.SystemOperator
            ? testDataDescription.ExampleWholesaleResultMessageDataForSystemOperator
            : testDataDescription.ExampleWholesaleResultMessageData;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var originalActor = (ActorNumber: delegatedFromRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : delegatedFromRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: delegatedFromRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);

        var gridAreaCode = exampleWholesaleResultMessageForActor.GridArea;
        var chargeCode = exampleWholesaleResultMessageForActor.ChargeCode;
        var chargeType = exampleWholesaleResultMessageForActor.ChargeType!;

        await GivenGridAreaOwnershipAsync(gridAreaCode, gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            gridAreaCode,
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Act
        // Original actor requests own data
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2023, 2, 1),
            periodEnd: (2023, 3, 1),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: chargeCode,
            chargeType: chargeType,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (gridAreaCode, transactionId),
            });

        // Assert
        var message = ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: null,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                chargeOwnerNumber.Value,
                new List<string> { gridAreaCode },
                null,
                new List<ChargeTypeInput> { new(chargeType.Name, chargeCode) }));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */

        // Arrange

        // Generate a mock ServiceBus Message with RequestCalculatedWholesaleServicesAcceptedV1 response from Process Manager,
        // based on the RequestCalculatedWholesaleServicesInputV1
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var requestCalculatedWholesaleServicesInputV1 = message!.ParseInput<RequestCalculatedWholesaleServicesInputV1>();
        var requestCalculatedWholesaleServicesAccepted = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(requestCalculatedWholesaleServicesInputV1);

        await GivenWholesaleServicesRequestAcceptedIsReceived(requestCalculatedWholesaleServicesAccepted);

        // Act
        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            peekDocumentFormat);

        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            peekDocumentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            delegatedActorPeekResults.Should().BeEmpty("because delegated actor shouldn't receive result when original actor made the request");
            peekResult = originalActorPeekResults.Should().ContainSingle("because there should only be one message for one grid area")
                .Subject;

            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
        }

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.WholesaleFixing,
                    null),
                ReceiverId: originalActor.ActorNumber.Value,
                ReceiverRole: originalActor.ActorRole,
                SenderId: DataHubDetails.DataHubActorNumber.Value, // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: chargeCode,
                ChargeType: chargeType,
                Currency: exampleWholesaleResultMessageForActor.Currency,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: exampleWholesaleResultMessageForActor.SettlementMethod,
                MeteringPointType: exampleWholesaleResultMessageForActor.MeteringPointType,
                GridArea: gridAreaCode,
                transactionId,
                PriceMeasurementUnit: exampleWholesaleResultMessageForActor.MeasurementUnit,
                ProductCode: "5790001330590",
                QuantityMeasurementUnit: exampleWholesaleResultMessageForActor.MeasurementUnit!,
                CalculationVersion: exampleWholesaleResultMessageForActor.Version,
                Resolution: exampleWholesaleResultMessageForActor.Resolution,
                Period: testDataDescription.Period,
                Points: exampleWholesaleResultMessageForActor.Points));
    }

    [Theory]
    [InlineData("Xml", DataHubNames.ActorRole.GridAccessProvider, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.GridAccessProvider, DataHubNames.ActorRole.Delegated)]
    [InlineData("Xml", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    [InlineData("Xml", DataHubNames.ActorRole.SystemOperator, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.SystemOperator, DataHubNames.ActorRole.Delegated)]
    public async Task AndGiven_RequestDoesNotContainOriginalActorNumber_When_DelegatedActorPeeksAllMessages_Then_DelegationIsUnsuccessfulSoRequestIsRejectedWithCorrectInvalidRoleError(string incomingDocumentFormatName, string originalActorRoleName, string delegatedToRoleName)
    {
        var incomingDocumentFormat = DocumentFormat.FromName(incomingDocumentFormatName);
        var originalActorRole = ActorRole.FromName(originalActorRoleName);
        var delegatedToRole = ActorRole.FromName(delegatedToRoleName);

        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = new Actor(ActorNumber.Create("1111111111111"), actorRole: originalActorRole);
        var delegatedToActor = new Actor(actorNumber: ActorNumber.Create("2222222222222"), actorRole: delegatedToRole);

        if (originalActor.ActorRole == ActorRole.GridAccessProvider)
            await GivenGridAreaOwnershipAsync("804", originalActor.ActorNumber);

        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenDelegation(
            originalActor,
            delegatedToActor,
            "804",
            ProcessType.RequestEnergyResults,
            GetNow());

        var response = await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2023, 12, 31),
            energySupplier: null,
            chargeOwner: null,
            chargeCode: null,
            chargeType: null,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            },
            assertRequestWasSuccessful: false);

        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeTrue("because a synchronous error should have occurred");
        response.MessageBody.Should().ContainAll("The authenticated user does not hold the required role");
        senderSpy.MessageSent.Should().BeFalse();
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML

