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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenWholesaleServicesRequestWithDelegationTests : WholesaleServicesBehaviourTestBase
{
    public GivenWholesaleServicesRequestWithDelegationTests(IntegrationTestFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
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
            roleCombinations.Add((ActorRole.GridOperator, ActorRole.Delegated));
            roleCombinations.Add((ActorRole.GridOperator, ActorRole.GridOperator));
        }

        var requestDocumentFormats = DocumentFormats
            .GetAllDocumentFormats(except: new[]
            {
                DocumentFormat.Xml.Name, // TODO: The CIM XML request feature isn't implemented yet
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

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task AndGiven_DelegationInOneGridArea_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: new[] { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

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
                SenderId: "5790001330552", // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "512",
                TransactionId.From("123564789123564789123564789123564787"),
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: wholesaleServicesRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea))] // Grid operator can't make request without grid area
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("609", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("704", gridOperatorNumber); // The delegated actor shouldn't receive data from this grid area

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: new[] { "512", "609" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

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
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle!, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            var seriesRequest = wholesaleServicesRequestAcceptedMessage.Series
                .Should().ContainSingle(request => request.GridArea == peekResultGridArea)
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
                    SenderId: "5790001330552",  // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerNumber.Value,
                    ChargeCode: "25361478",
                    ChargeType: ChargeType.Tariff,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierNumber.Value,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridArea: seriesRequest.GridArea,
                    TransactionId.From("123564789123564789123564789123564787"),
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

        resultGridAreas.Should().BeEquivalentTo("512", "609");
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task AndGiven_InvalidRequestWithDelegationInOneGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectWholesaleSettlementDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

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
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: new[] { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2023, 12, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestRejectedMessage = WholesaleServicesResponseEventBuilder
            .GenerateRejectedFrom(message.WholesaleServicesRequest);

        await GivenWholesaleServicesRequestRejectedIsReceived(message.ProcessId, wholesaleServicesRequestRejectedMessage);

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

        var expectedReasonMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse"
                                    + " med en engrosfiksering eller korrektioner / It is only possible to request"
                                    + " data for a full month in relation to wholesalefixing or corrections";

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
                "123564789123564789123564789123564787",
                "E17",
                expectedReasonMessage));
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
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("609", gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow());

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
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: new[] { "512", "609" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2023, 12, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestRejectedMessage = WholesaleServicesResponseEventBuilder
            .GenerateRejectedFrom(message.WholesaleServicesRequest);

        await GivenWholesaleServicesRequestRejectedIsReceived(message.ProcessId, wholesaleServicesRequestRejectedMessage);

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

        var expectedReasonMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse"
                                    + " med en engrosfiksering eller korrektioner / It is only possible to request"
                                    + " data for a full month in relation to wholesalefixing or corrections";

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
                "123564789123564789123564789123564787",
                "E17",
                expectedReasonMessage));
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea))] // Grid operator can't make request without grid area
    public async Task AndGiven_OriginalActorRequestsOwnDataWithDataInTwoGridAreas_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("973", gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Original actor requests own data
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: Array.Empty<string>(),
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow(), defaultGridAreas: new List<string> { "512", "973" });

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

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
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle!, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            var seriesRequest = wholesaleServicesRequestAcceptedMessage.Series
                .Should().ContainSingle(request => request.GridArea == peekResultGridArea)
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
                    SenderId: "5790001330552",  // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: chargeOwnerNumber.Value,
                    ChargeCode: "25361478",
                    ChargeType: ChargeType.Tariff,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: energySupplierNumber.Value,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridArea: seriesRequest.GridArea,
                    TransactionId.From("123564789123564789123564789123564787"),
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

        resultGridAreas.Should().BeEquivalentTo("512", "973");
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations))]
    public async Task AndGiven_OriginalActorRequestsOwnDataWithGridArea_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
        var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = originalActor.ActorRole == ActorRole.SystemOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("5799999933444");
        var gridOperatorNumber = originalActor.ActorRole == ActorRole.GridOperator
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Original actor requests own data
        await GivenReceivedWholesaleServicesRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            chargeOwner: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: new[] { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, "25361478"),
                },
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

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
                SenderId: "5790001330552", // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "512",
                TransactionId.From("123564789123564789123564789123564787"),
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: wholesaleServicesRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    }

    // TODO: Remove skip when Wholesale synchronous validation is implemented
    // TODO: Add "Xml" tests when CIM XML is supported for Wholesale requests
    [Theory(Skip = "Skipped until Wholesale synchronous validation is implemented")]
    // [InlineData("Xml", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.Delegated)]
    // [InlineData("Xml", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    // [InlineData("Xml", DataHubNames.ActorRole.SystemOperator, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.SystemOperator, DataHubNames.ActorRole.Delegated)]
    // [InlineData("Xml", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.GridOperator)]
    [InlineData("Json", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.GridOperator)]
    public async Task AndGiven_RequestDoesNotContainOriginalActorNumber_When_DelegatedActorPeeksAllMessages_Then_DelegationIsUnsuccessfulSoRequestIsRejectedWithCorrectInvalidRoleError(string incomingDocumentFormatName, string originalActorRoleName, string delegatedToRoleName)
    {
        var incomingDocumentFormat = DocumentFormat.FromName(incomingDocumentFormatName);
        var originalActorRole = ActorRole.FromName(originalActorRoleName);
        var delegatedToRole = ActorRole.FromName(delegatedToRoleName);

        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = new Actor(ActorNumber.Create("1111111111111"), ActorRole: originalActorRole);
        var delegatedToActor = new Actor(ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);

        if (originalActor.ActorRole == ActorRole.GridOperator)
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

