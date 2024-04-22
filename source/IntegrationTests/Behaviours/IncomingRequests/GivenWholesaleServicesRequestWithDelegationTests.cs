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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
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
public class GivenWholesaleServicesRequestWithDelegationTests : BehavioursTestBase
{
    public GivenWholesaleServicesRequestWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static object[][] DocumentFormatsWithDelegatedFromAndToRoles()
    {
        // TODO: Who can delegate RequestWholesaleServices? We assume it's only the actors who can actually
        // perform the RequestWholesaleServices, eg. DDQ and DDM
        var delegatedFromRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.GridOperator,
        };

        var delegatedToRoles = new List<ActorRole>
        {
            ActorRole.Delegated,
            ActorRole.GridOperator,
        };

        return DocumentFormats
            .GetAllDocumentFormats(except: new[] { DocumentFormat.Xml.Name, DocumentFormat.Ebix.Name })
            .SelectMany(df => delegatedFromRoles
                .SelectMany(from => delegatedToRoles
                    .Select(to => new object[] { df, from, to })))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithDelegatedFromAndToRoles))]
    public async Task AndGiven_DelegationInOneGridArea_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(DocumentFormat documentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create(WholesaleServicesResponseEventBuilder.DefaultChargeOwnerId);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: documentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            gridArea: "512",
            energySupplierActorNumber: energySupplierNumber,
            chargeOwnerActorNumber: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            transactionId: "123564789123564789123564789123564787",
            isMonthly: false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512" },
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplierId: energySupplierNumber.Value,
            chargeOwnerId: chargeOwnerNumber.Value,
            resolution: null,
            businessReason: DataHubNames.BusinessReason.WholesaleFixing,
            chargeTypes: new List<(string ChargeType, string ChargeCode)>
            {
                (DataHubNames.ChargeType.Tariff, "25361478"),
            },
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null);

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateWholesaleServicesRequestAccepted(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            documentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

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
            documentFormat,
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
                OriginalTransactionIdReference: "123564789123564789123564789123564787",
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
    [MemberData(nameof(DocumentFormatsWithDelegatedFromAndToRoles))]
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentsWithCorrectContent(DocumentFormat documentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create(WholesaleServicesResponseEventBuilder.DefaultChargeOwnerId);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenReceivedWholesaleServicesRequest(
            documentFormat: documentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            gridArea: null,
            energySupplierActorNumber: energySupplierNumber,
            chargeOwnerActorNumber: chargeOwnerNumber,
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            transactionId: "123564789123564789123564789123564787",
            isMonthly: false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplierId: energySupplierNumber.Value,
            chargeOwnerId: chargeOwnerNumber.Value,
            resolution: null,
            businessReason: DataHubNames.BusinessReason.WholesaleFixing,
            chargeTypes: new List<(string ChargeType, string ChargeCode)>
            {
                (DataHubNames.ChargeType.Tariff, "25361478"),
            },
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null);

        // TODO: Assert correct process is created

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateWholesaleServicesRequestAccepted(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            documentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        // Assert
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            delegatedActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        foreach (var peekResult in delegatedActorPeekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle!, documentFormat);

            var seriesRequest = wholesaleServicesRequestAcceptedMessage.Series
                .Should().ContainSingle(request => request.GridArea == peekResultGridArea)
                .Subject;

            await ThenNotifyWholesaleServicesDocumentIsCorrect(
                peekResult.Bundle,
                documentFormat,
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
                    OriginalTransactionIdReference: "123564789123564789123564789123564787",
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
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithDelegatedFromAndToRoles))]
    public async Task AndGiven_InvalidRequestWithDelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectWholesaleSettlementDocumentsWithCorrectContent(DocumentFormat documentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create(WholesaleServicesResponseEventBuilder.DefaultChargeOwnerId);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Setup fake request (period end is before period start)
        await GivenReceivedWholesaleServicesRequest(
            documentFormat,
            delegatedToActor.ActorNumber,
            originalActor.ActorRole,
            (2024, 01, 01),
            (2023, 12, 31),
            null,
            energySupplierNumber,
            chargeOwnerNumber,
            "25361478",
            ChargeType.Tariff,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplierId: energySupplierNumber.Value,
            chargeOwnerId: chargeOwnerNumber.Value,
            resolution: null,
            businessReason: DataHubNames.BusinessReason.WholesaleFixing,
            chargeTypes: new List<(string ChargeType, string ChargeCode)>
            {
                (DataHubNames.ChargeType.Tariff, "25361478"),
            },
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2023, 12, 31)),
            null);

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestRejectedMessage = WholesaleServicesResponseEventBuilder
            .GenerateWholesaleServicesRequestRejected(message.WholesaleServicesRequest);

        await GivenWholesaleServicesRequestRejectedIsReceived(message.ProcessId, wholesaleServicesRequestRejectedMessage);

        // Act
        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            documentFormat);

        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

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
            documentFormat,
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
        // document => document
        //     // -- Assert header values --
        //     .MessageIdExists()
        //     // Assert Type? ("ERR")
        //     .HasBusinessReason(BusinessReason.WholesaleFixing)
        //     // Assert businessSector.type? (23)
        //     .HasSenderId("5790001330552")
        //     .HasSenderRole(ActorRole.MeteredDataAdministrator) // Example says "DDZ", but document writes as "DGL"?
        //     .HasReceiverId(delegatedToActor.ActorNumber.Value)
        //     .HasReceiverRole(originalActor.ActorRole)
        //     .HasTimestamp()
        //     .HasReasonCode(ReasonCode.FullyRejected.Code) // A02 = Rejected
        //     .TransactionIdExists()
        //     .HasOriginalTransactionId("123564789123564789123564789123564787")
        //     .HasSerieReasonCode("E17") // E17 = Invalid period length
        //     .HasSerieReasonMessage("Det er kun muligt at anmode om data på for en hel måned i forbindelse"
        //                            + " med en engrosfiksering eller korrektioner / It is only possible to request"
        //                            + " data for a full month in relation to wholesalefixing or corrections"));
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithDelegatedFromAndToRoles))]
    [MemberData(nameof(DocumentFormatsWithDelegatedFromAndToRoles))]
    public async Task AndGiven_OriginalActorRequestsOwnData_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(DocumentFormat documentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
            ? originalActor.ActorNumber
            : ActorNumber.Create(WholesaleServicesResponseEventBuilder.DefaultChargeOwnerId);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(originalActor.ActorNumber, originalActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Original actor requests own data
        await GivenReceivedWholesaleServicesRequest(
            documentFormat,
            originalActor.ActorNumber,
            originalActor.ActorRole,
            (2024, 1, 1),
            (2024, 1, 31),
            "512",
            energySupplierNumber,
            chargeOwnerNumber,
            "25361478",
            ChargeType.Tariff,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512" },
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplierId: energySupplierNumber.Value,
            chargeOwnerId: chargeOwnerNumber.Value,
            resolution: null,
            businessReason: DataHubNames.BusinessReason.WholesaleFixing,
            chargeTypes: new List<(string ChargeType, string ChargeCode)>
            {
                (DataHubNames.ChargeType.Tariff, "25361478"),
            },
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null);

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var wholesaleServicesRequestAcceptedMessage = WholesaleServicesResponseEventBuilder
            .GenerateWholesaleServicesRequestAccepted(message.WholesaleServicesRequest, GetNow());

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleServicesRequestAcceptedMessage);

        // Act
        var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        var originalActorPeekResults = await WhenActorPeeksAllMessages(
            originalActor.ActorNumber,
            originalActor.ActorRole,
            documentFormat);

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
            documentFormat,
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
                OriginalTransactionIdReference: "123564789123564789123564789123564787",
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

    private async Task<string> GetGridAreaFromNotifyWholesaleServicesDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var xmlDocument = await XDocument.LoadAsync(documentStream, LoadOptions.None, CancellationToken.None);

            var gridAreaEbixElement = xmlDocument.Root!
                .XPathSelectElement("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification");

            gridAreaEbixElement.Should().NotBeNull("because grid area should be present in the ebIX document");
            return gridAreaEbixElement!.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlDocument = await XDocument.LoadAsync(documentStream, LoadOptions.None, CancellationToken.None);

            var gridAreaCimXmlElement = cimXmlDocument.Root!
                .XPathSelectElement("Series[1]/meteringGridArea_Domain.mRID");

            gridAreaCimXmlElement.Should().NotBeNull("because grid area should be present in the CIM XML document");
            return gridAreaCimXmlElement!.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var gridAreaCimJsonElement = cimJsonDocument.RootElement
                .GetProperty("NotifyWholesaleServices_MarketDocument")
                .GetProperty("Series").EnumerateArray().ToList()
                .Single()
                .GetProperty("meteringGridArea_Domain.mRID")
                .GetProperty("value");

            gridAreaCimJsonElement.Should().NotBeNull("because grid area should be present in the CIM JSON document");
            return gridAreaCimJsonElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    private Task GivenWholesaleServicesRequestAcceptedIsReceived(Guid processId, WholesaleServicesRequestAccepted acceptedMessage)
    {
        return GivenWholesaleServicesRequestResponseIsReceived(processId, acceptedMessage);
    }

    private Task GivenWholesaleServicesRequestRejectedIsReceived(Guid processId, WholesaleServicesRequestRejected rejectedMessage)
    {
        return GivenWholesaleServicesRequestResponseIsReceived(processId, rejectedMessage);
    }

    private Task GivenWholesaleServicesRequestResponseIsReceived<TType>(Guid processId, TType wholesaleServicesRequestResponseMessage)
        where TType : IMessage
    {
        return HavingReceivedInboxEventAsync(
            eventType: typeof(TType).Name,
            eventPayload: wholesaleServicesRequestResponseMessage,
            processId: processId);
    }

    private Task<(WholesaleServicesRequest WholesaleServicesRequest, Guid ProcessId)> ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            ServiceBusSenderSpy senderSpy,
            IReadOnlyCollection<string> gridAreas,
            string requestedForActorNumber,
            string requestedForActorRole,
            string? energySupplierId,
            string? chargeOwnerId,
            string? resolution,
            string businessReason,
            List<(string ChargeType, string ChargeCode)>? chargeTypes,
            Period period,
            string? settlementVersion)
    {
        using (new AssertionScope())
        {
            senderSpy.MessageSent.Should().BeTrue();
            senderSpy.Message.Should().NotBeNull();
        }

        var serviceBusMessage = senderSpy.Message!;
        Guid processId;
        using (new AssertionScope())
        {
            serviceBusMessage.Subject.Should().Be(nameof(WholesaleServicesRequest));
            serviceBusMessage.Body.Should().NotBeNull();
            serviceBusMessage.ApplicationProperties.TryGetValue("ReferenceId", out var referenceId);
            referenceId.Should().NotBeNull();
            Guid.TryParse(referenceId!.ToString()!, out processId).Should().BeTrue();
        }

        var wholesaleServicesRequestMessage = WholesaleServicesRequest.Parser.ParseFrom(serviceBusMessage.Body);
        wholesaleServicesRequestMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        wholesaleServicesRequestMessage.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        wholesaleServicesRequestMessage.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        wholesaleServicesRequestMessage.RequestedForActorRole.Should().Be(requestedForActorRole);

        if (energySupplierId == null)
            wholesaleServicesRequestMessage.HasEnergySupplierId.Should().BeFalse();
        else
            wholesaleServicesRequestMessage.EnergySupplierId.Should().Be(energySupplierId);

        if (chargeOwnerId == null)
            wholesaleServicesRequestMessage.HasChargeOwnerId.Should().BeFalse();
        else
            wholesaleServicesRequestMessage.ChargeOwnerId.Should().Be(chargeOwnerId);

        if (resolution == null)
            wholesaleServicesRequestMessage.HasResolution.Should().BeFalse();
        else
            wholesaleServicesRequestMessage.Resolution.Should().Be(resolution);

        wholesaleServicesRequestMessage.BusinessReason.Should().Be(businessReason);

        if (chargeTypes == null)
        {
            wholesaleServicesRequestMessage.ChargeTypes.Should().BeEmpty();
        }
        else
        {
            wholesaleServicesRequestMessage.ChargeTypes.Should().BeEquivalentTo(chargeTypes.Select(ct => new Energinet.DataHub.Edi.Requests.ChargeType
            {
                ChargeType_ = ct.ChargeType,
                ChargeCode = ct.ChargeCode,
            }));
        }

        wholesaleServicesRequestMessage.PeriodStart.Should().Be(period.Start.ToString());
        wholesaleServicesRequestMessage.PeriodEnd.Should().Be(period.End.ToString());

        if (settlementVersion == null)
            wholesaleServicesRequestMessage.HasSettlementVersion.Should().BeFalse();
        else
            wholesaleServicesRequestMessage.SettlementVersion.Should().Be(settlementVersion);

        return Task.FromResult((wholesaleServicesRequestMessage, processId));
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML

