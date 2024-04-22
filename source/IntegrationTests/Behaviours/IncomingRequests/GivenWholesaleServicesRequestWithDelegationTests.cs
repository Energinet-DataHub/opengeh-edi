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
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyWholesaleServices;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using ChargeType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ChargeType;
using Duration = NodaTime.Duration;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
public class GivenWholesaleServicesRequestWithDelegationTests : BehavioursTestBase
{
    public GivenWholesaleServicesRequestWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormatsWithActorRolesExcept), new[] { "Xml", "Ebix" }, new[] { ActorRole.EnergySupplierCode, ActorRole.DelegatedCode }, MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_DelegationInOneGridArea_When_ActorPeeksAllMessages_Then_OneNotifyWholesaleServicesDocumentIsCreatedCorrectly(DocumentFormat documentFormat, ActorRole delegatedToRole)
    {
        /*
         * A request is a test with 2 parts:
         *  1. Send a request to the system (incoming message)
         *  2. Receive data from Wholesale and create RSM document (outgoing message)
         */

        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var delegatedByActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenReceivedWholesaleServicesRequest(
            documentFormat,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 1, 31),
            "512",
            delegatedByActor.ActorNumber.Value,
            "5799999933444",
            "25361478",
            ChargeType.Tariff.Code,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512" },
            requestedForActorNumber: "1111111111111",
            requestedForActorRole: DataHubNames.ActorRole.EnergySupplier,
            energySupplierId: "1111111111111");

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
        var peekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        // Assert
        var peekResult = peekResults.Should().ContainSingle("because there should only be one message for one grid area")
            .Subject;

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenNotifyWholesaleServicesDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            new NotifyWholesaleServicesDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.WholesaleFixing,
                    null),
                ReceiverId: "2222222222222",
                ReceiverRole: ActorRole.EnergySupplier,
                SenderId: "5790001330552", // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: "5799999933444",
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: "1111111111111",
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
    [MemberData(nameof(DocumentFormats.AllDocumentFormatsWithActorRolesExcept), new object[] { new[] { "Xml", "Ebix" }, new[] { ActorRole.EnergySupplierCode, ActorRole.DelegatedCode } }, MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_TwoNotifyWholesaleServicesDocumentsAreCreatedCorrectly(DocumentFormat documentFormat, ActorRole delegatedToRole)
    {
        /*
         * A request is a test with 2 parts:
         *  1. Send a request to the system (incoming message)
         *  2. Receive data from Wholesale and create RSM document (outgoing message)
         */

        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var delegatedByActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenReceivedWholesaleServicesRequest(
            documentFormat,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 1, 1),
            (2024, 1, 31),
            null,
            delegatedByActor.ActorNumber.Value,
            "5799999933444",
            "25361478",
            ChargeType.Tariff.Code,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: "1111111111111",
            requestedForActorRole: DataHubNames.ActorRole.EnergySupplier,
            energySupplierId: "1111111111111");

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
        var peekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        // Assert
        peekResults.Should().HaveCount(2, "because there should be one message for each grid area");

        foreach (var peekResult in peekResults)
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
                    ReceiverId: "2222222222222",
                    ReceiverRole: ActorRole.EnergySupplier,
                    SenderId: "5790001330552",  // Sender is always DataHub
                    SenderRole: ActorRole.MeteredDataAdministrator,
                    ChargeTypeOwner: "5799999933444",
                    ChargeCode: "25361478",
                    ChargeType: ChargeType.Tariff,
                    Currency: Currency.DanishCrowns,
                    EnergySupplierNumber: "1111111111111",
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

            // document => document
            //         // -- Assert header values --
            //         .MessageIdExists()
            //         // Assert businessSector.type? (23)
            //         .HasTimestamp("2024-07-01T14:57:09Z")
            //         .HasBusinessReason(BusinessReason.WholesaleFixing, CodeListType.EbixDenmark)
            //         .HasReceiverId(ActorNumber.Create("2222222222222"))
            //         .HasReceiverRole(ActorRole.EnergySupplier, CodeListType.Ebix)
            //         .HasSenderId(ActorNumber.Create("5790001330552"), "A10") // Sender is DataHub
            //         .HasSenderRole(ActorRole.MeteredDataAdministrator)
            //         // Assert type? (E31)
            //         // -- Assert series values --
            //         .TransactionIdExists()
            //         .HasChargeTypeOwner(ActorNumber.Create("5799999933444"), "A10")
            //         .HasChargeCode("25361478")
            //         .HasChargeType(BuildingBlocks.Domain.Models.ChargeType.Tariff)
            //         .HasCurrency(Currency.DanishCrowns)
            //         .HasEnergySupplierNumber(ActorNumber.Create("1111111111111"), "A10")
            //         .HasSettlementMethod(SettlementMethod.Flex)
            //         .HasMeteringPointType(MeteringPointType.Consumption)
            //         .HasGridAreaCode(seriesRequest.GridArea, "NDK")
            //         .HasOriginalTransactionIdReference("123564789123564789123564789123564787")
            //         .HasPriceMeasurementUnit(MeasurementUnit.Kwh)
            //         .HasProductCode("5790001330590") // Example says "8716867000030", but document writes as "5790001330590"?
            //         .HasQuantityMeasurementUnit(MeasurementUnit.Kwh)
            //         .SettlementVersionDoesNotExist()
            //         .HasCalculationVersion(GetNow().ToUnixTimeTicks())
            //         .HasResolution(Resolution.Hourly)
            //         .HasPeriod(
            //             new BuildingBlocks.Domain.Models.Period(
            //                 CreateDateInstant(2024, 1, 1),
            //                 CreateDateInstant(2024, 1, 31)))
            //         .HasPoints(seriesRequest.TimeSeriesPoints));
        }
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormatsWithActorRolesExcept), new object[] { new[] { "Xml", "Ebix" }, new[] { ActorRole.EnergySupplierCode, ActorRole.DelegatedCode } }, MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_InvalidRequestWithDelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_OneRejectWholesaleSettlementDocumentsIsCreatedCorrectly(DocumentFormat documentFormat, ActorRole delegatedToRole)
    {
        /*
         * A request is a test with 2 parts:
         *  1. Send a request to the system (incoming message)
         *  2. Receive data from Wholesale and create RSM document (outgoing message)
         */

        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var delegatedByActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        await GivenDelegation(
            new ActorNumberAndRoleDto(delegatedByActor.ActorNumber, delegatedByActor.ActorRole),
            new ActorNumberAndRoleDto(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestWholesaleResults,
            GetNow(),
            GetNow().Plus(Duration.FromDays(32)));

        // Setup fake request (period end is before period start)
        await GivenReceivedWholesaleServicesRequest(
            documentFormat,
            delegatedToActor.ActorNumber.Value,
            delegatedByActor.ActorRole.Code,
            (2024, 01, 01),
            (2023, 12, 31),
            null,
            delegatedByActor.ActorNumber.Value,
            "5799999933444",
            "25361478",
            BuildingBlocks.Domain.Models.ChargeType.Tariff.Code,
            "123564789123564789123564789123564787",
            false);

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512", "609" },
            requestedForActorNumber: "1111111111111",
            requestedForActorRole: "EnergySupplier",
            energySupplierId: "1111111111111");

        // TODO: Assert correct process is created

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
        var peekResults = await WhenActorPeeksAllMessages(
            delegatedToActor.ActorNumber,
            delegatedToActor.ActorRole,
            documentFormat);

        // Assert
        // Assert
        var peekResult = peekResults.Should().ContainSingle("because there should only be one message for one grid area")
            .Subject;

        peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");

        await ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
            peekResult.Bundle,
            documentFormat,
            document => document
                // -- Assert header values --
                .MessageIdExists()
                // Assert Type? ("ERR")
                .HasBusinessReason(BusinessReason.WholesaleFixing)
                // Assert businessSector.type? (23)
                .HasSenderId("5790001330552")
                .HasSenderRole(ActorRole.MeteredDataAdministrator) // Example says "DDZ", but document writes as "DGL"?
                .HasReceiverId("2222222222222")
                .HasReceiverRole(ActorRole.EnergySupplier)
                .HasTimestamp(InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value)
                .HasReasonCode(ReasonCode.FullyRejected.Code) // A02 = Rejected
                .TransactionIdExists()
                .HasOriginalTransactionId("123564789123564789123564789123564787")
                .HasSerieReasonCode("E17") // E17 = Invalid period length
                .HasSerieReasonMessage("Det er kun muligt at anmode om data på for en hel måned i forbindelse"
                                       + " med en engrosfiksering eller korrektioner / It is only possible to request"
                                       + " data for a full month in relation to wholesalefixing or corrections"));
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
        string energySupplierId)
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
        wholesaleServicesRequestMessage.EnergySupplierId.Should().Be(energySupplierId);

        return Task.FromResult((wholesaleServicesRequestMessage, processId));
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML

