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
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenAggregatedMeasureDataRequestWithDelegationTests : BehavioursTestBase
{
    public GivenAggregatedMeasureDataRequestWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static object[][] DocumentFormatsWithDelegationCombinations()
    {
        // The actor roles who can perform AggregatedMeasureDataRequest's
        var delegatedFromRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.MeteredDataResponsible,
            ActorRole.BalanceResponsibleParty,
            ActorRole.GridOperator, // Grid Operator can make requests because of DDM -> MDR hack
        };

        var delegatedToRoles = new List<ActorRole>
        {
            ActorRole.Delegated,
            ActorRole.GridOperator,
        };

        var exceptForIncomingDocumentFormats = new[]
        {
            DocumentFormat.Xml.Name, // TODO: Implement XML
            DocumentFormat.Ebix.Name, // ebIX is not supported for requests
        };

        return DocumentFormats
            .GetAllDocumentFormats(exceptForIncomingDocumentFormats)
            .SelectMany(incomingDocumentFormat => delegatedFromRoles
                .SelectMany(delegatedFromRole => delegatedToRoles
                    .SelectMany(delegatedToRole => DocumentFormats.GetAllDocumentFormats()
                        .Select(peekDocumentFormat => new object[]
                        {
                            incomingDocumentFormat,
                            peekDocumentFormat,
                            delegatedFromRole,
                            delegatedToRole,
                        }))))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithDelegationCombinations))]
    public async Task AndGiven_DelegationInOneGridArea_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneNotifyAggregatedMeasureDataWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var balanceResponsibleParty = originalActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            gridArea: "512",
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            transactionId: "123564789123564789123564789123564787");

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new[] { "512" },
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplier: energySupplierNumber.Value,
            balanceResponsibleParty: balanceResponsibleParty.Value,
            businessReason: DataHubNames.BusinessReason.WholesaleFixing,
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null,
            settlementMethod: SettlementMethod.Flex.Code,
            meteringPointType: MeteringPointType.Consumption.Code);

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var aggregatedMeasureDataRequestAcceptedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow());

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, aggregatedMeasureDataRequestAcceptedMessage);

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

        await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new NotifyAggregatedMeasureDataDocumentAssertionInput(
                Timestamp: "2024-07-01T14:57:09Z",
                BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
                    BusinessReason.BalanceFixing,
                    null),
                ReceiverId: delegatedToActor.ActorNumber,
                //ReceiverRole: originalActor.ActorRole,
                SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                //SenderRole: ActorRole.MeteredDataAdministrator,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridAreaCode: "512",
                OriginalTransactionIdReference: "123564789123564789123564789123564787",
                ProductCode: "8716867000030", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: aggregatedMeasureDataRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    }

    // [Theory]
    // [MemberData(nameof(DocumentFormatsWithDelegationCombinations))]
    // public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyAggregatedMeasureDataDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    // {
    //     /*
    //      *  --- PART 1: Receive request, create process and send message to Wholesale ---
    //      */
    //
    //     // Arrange
    //     var senderSpy = CreateServiceBusSenderSpy();
    //     var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
    //     var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
    //     var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("3333333333333");
    //     var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("5799999933444");
    //
    //     GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
    //     GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
    //
    //     await GivenDelegation(
    //         new(originalActor.ActorNumber, originalActor.ActorRole),
    //         new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "512",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow());
    //
    //     await GivenDelegation(
    //         new(originalActor.ActorNumber, originalActor.ActorRole),
    //         new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "609",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow());
    //
    //     await GivenReceivedAggregatedMeasureDataRequest(
    //         documentFormat: incomingDocumentFormat,
    //         senderActorNumber: delegatedToActor.ActorNumber,
    //         senderActorRole: originalActor.ActorRole,
    //         periodStart: (2024, 1, 1),
    //         periodEnd: (2024, 1, 31),
    //         gridArea: null,
    //         energySupplier: energySupplierNumber,
    //         chargeOwnerActorNumber: chargeOwnerNumber,
    //         chargeCode: "25361478",
    //         chargeType: ChargeType.Tariff,
    //         transactionId: "123564789123564789123564789123564787",
    //         isMonthly: false);
    //
    //     // Act
    //     await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);
    //
    //     // Assert
    //     var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
    //         senderSpy,
    //         gridAreas: new[] { "512", "609" },
    //         requestedForActorNumber: originalActor.ActorNumber.Value,
    //         requestedForActorRole: originalActor.ActorRole.Name,
    //         energySupplier: energySupplierNumber.Value,
    //         chargeOwnerId: chargeOwnerNumber.Value,
    //         resolution: null,
    //         businessReason: DataHubNames.BusinessReason.WholesaleFixing,
    //         chargeTypes: new List<(string ChargeType, string ChargeCode)>
    //         {
    //             (DataHubNames.ChargeType.Tariff, "25361478"),
    //         },
    //         new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
    //         null);
    //
    //     // TODO: Assert correct process is created
    //
    //     /*
    //      *  --- PART 2: Receive data from Wholesale and create RSM document ---
    //      */
    //
    //     // Arrange
    //
    //     // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
    //     // It is very important that the generated data is correct,
    //     // since (almost) all assertion after this point is based on this data
    //     var aggregatedMeasureDataRequestAcceptedMessage = AggregatedMeasureDataResponseEventBuilder
    //         .GenerateAggregatedMeasureDataRequestAccepted(message.AggregatedTimeSeriesRequest, GetNow());
    //
    //     await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, aggregatedMeasureDataRequestAcceptedMessage);
    //
    //     // Act
    //     var originalActorPeekResults = await WhenActorPeeksAllMessages(
    //         originalActor.ActorNumber,
    //         originalActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
    //         delegatedToActor.ActorNumber,
    //         delegatedToActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     // Assert
    //     using (new AssertionScope())
    //     {
    //         originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
    //         delegatedActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
    //     }
    //
    //     foreach (var peekResult in delegatedActorPeekResults)
    //     {
    //         peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
    //         var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle!, peekDocumentFormat);
    //
    //         var seriesRequest = aggregatedMeasureDataRequestAcceptedMessage.Series
    //             .Should().ContainSingle(request => request.GridArea == peekResultGridArea)
    //             .Subject;
    //
    //         await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
    //             peekResult.Bundle,
    //             peekDocumentFormat,
    //             new NotifyAggregatedMeasureDataDocumentAssertionInput(
    //                 Timestamp: "2024-07-01T14:57:09Z",
    //                 BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
    //                     BusinessReason.WholesaleFixing,
    //                     null),
    //                 ReceiverId: delegatedToActor.ActorNumber.Value,
    //                 ReceiverRole: originalActor.ActorRole,
    //                 SenderId: "5790001330552",  // Sender is always DataHub
    //                 SenderRole: ActorRole.MeteredDataAdministrator,
    //                 ChargeTypeOwner: chargeOwnerNumber.Value,
    //                 ChargeCode: "25361478",
    //                 ChargeType: ChargeType.Tariff,
    //                 Currency: Currency.DanishCrowns,
    //                 EnergySupplierNumber: energySupplierNumber.Value,
    //                 SettlementMethod: SettlementMethod.Flex,
    //                 MeteringPointType: MeteringPointType.Consumption,
    //                 GridArea: seriesRequest.GridArea,
    //                 OriginalTransactionIdReference: "123564789123564789123564789123564787",
    //                 PriceMeasurementUnit: MeasurementUnit.Kwh,
    //                 ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
    //                 QuantityMeasurementUnit: MeasurementUnit.Kwh,
    //                 CalculationVersion: GetNow().ToUnixTimeTicks(),
    //                 Resolution: Resolution.Hourly,
    //                 Period: new Period(
    //                     CreateDateInstant(2024, 1, 1),
    //                     CreateDateInstant(2024, 1, 31)),
    //                 Points: seriesRequest.TimeSeriesPoints));
    //     }
    // }
    //
    // /// <summary>
    // /// Rejected document based on example:
    // ///     https://energinet.sharepoint.com/sites/DH3ART-team/_layouts/15/download.aspx?UniqueId=60f1449eb8f44b179f233dda432b8f65&e=uVle0k
    // /// </summary>
    // [Theory]
    // [MemberData(nameof(DocumentFormatsWithDelegationCombinations))]
    // public async Task AndGiven_InvalidRequestWithDelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectAggregatedMeasureDataDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    // {
    //     /*
    //      *  --- PART 1: Receive request, create process and send message to Wholesale ---
    //      */
    //
    //     // Arrange
    //     var senderSpy = CreateServiceBusSenderSpy();
    //     var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
    //     var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
    //     var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("3333333333333");
    //     var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("5799999933444");
    //
    //     GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
    //     GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
    //
    //     await GivenDelegation(
    //         new(originalActor.ActorNumber, originalActor.ActorRole),
    //         new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "512",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow());
    //
    //     await GivenDelegation(
    //         new(originalActor.ActorNumber, originalActor.ActorRole),
    //         new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "609",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow());
    //
    //     // Setup fake request (period end is before period start)
    //     await GivenReceivedAggregatedMeasureDataRequest(
    //         documentFormat: incomingDocumentFormat,
    //         senderActorNumber: delegatedToActor.ActorNumber,
    //         senderActorRole: originalActor.ActorRole,
    //         periodStart: (2024, 01, 01),
    //         periodEnd: (2023, 12, 31),
    //         gridArea: null,
    //         energySupplier: energySupplierNumber,
    //         chargeOwnerActorNumber: chargeOwnerNumber,
    //         chargeCode: "25361478",
    //         chargeType: ChargeType.Tariff,
    //         transactionId: "123564789123564789123564789123564787",
    //         isMonthly: false);
    //
    //     // Act
    //     await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);
    //
    //     // Assert
    //     var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
    //         senderSpy,
    //         gridAreas: new[] { "512", "609" },
    //         requestedForActorNumber: originalActor.ActorNumber.Value,
    //         requestedForActorRole: originalActor.ActorRole.Name,
    //         energySupplier: energySupplierNumber.Value,
    //         chargeOwnerId: chargeOwnerNumber.Value,
    //         resolution: null,
    //         businessReason: DataHubNames.BusinessReason.WholesaleFixing,
    //         chargeTypes: new List<(string ChargeType, string ChargeCode)>
    //         {
    //             (DataHubNames.ChargeType.Tariff, "25361478"),
    //         },
    //         new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2023, 12, 31)),
    //         null);
    //
    //     // TODO: Assert correct process is created?
    //
    //     /*
    //      *  --- PART 2: Receive data from Wholesale and create RSM document ---
    //      */
    //
    //     // Arrange
    //
    //     // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
    //     // It is very important that the generated data is correct,
    //     // since (almost) all assertion after this point is based on this data
    //     var aggregatedMeasureDataRequestRejectedMessage = AggregatedMeasureDataResponseEventBuilder
    //         .GenerateAggregatedMeasureDataRequestRejected(message.AggregatedTimeSeriesRequest);
    //
    //     await GivenAggregatedMeasureDataRequestRejectedIsReceived(message.ProcessId, aggregatedMeasureDataRequestRejectedMessage);
    //
    //     // Act
    //     var originalActorPeekResults = await WhenActorPeeksAllMessages(
    //         originalActor.ActorNumber,
    //         originalActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
    //         delegatedToActor.ActorNumber,
    //         delegatedToActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     // Assert
    //     PeekResultDto peekResult;
    //     using (new AssertionScope())
    //     {
    //         originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
    //         peekResult = delegatedActorPeekResults.Should().ContainSingle("because there should only be one rejected message regardless of grid areas")
    //             .Subject;
    //
    //         peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
    //     }
    //
    //     var expectedReasonMessage = "Det er kun muligt at anmode om data på for en hel måned i forbindelse"
    //                                 + " med en engrosfiksering eller korrektioner / It is only possible to request"
    //                                 + " data for a full month in relation to wholesalefixing or corrections";
    //
    //     await ThenRejectRequestWholesaleSettlementDocumentIsCorrect(
    //         peekResult.Bundle,
    //         peekDocumentFormat,
    //         new RejectRequestWholesaleSettlementDocumentAssertionInput(
    //             InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
    //             BusinessReason.WholesaleFixing,
    //             delegatedToActor.ActorNumber.Value,
    //             originalActor.ActorRole,
    //             "5790001330552",
    //             ActorRole.MeteredDataAdministrator,
    //             ReasonCode.FullyRejected.Code,
    //             "123564789123564789123564789123564787",
    //             "E17",
    //             expectedReasonMessage));
    // }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithDelegationCombinations))]
    public async Task AndGiven_OriginalActorRequestsOwnDataWithDataInTwoGridAreas_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesTwoNotifyAggregatedMeasureDataDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var balanceResponsibleParty = originalActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? originalActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestWholesaleResults,
            GetNow());

        // Original actor requests own data
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: originalActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            gridArea: null,
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            transactionId: "123564789123564789123564789123564787");

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new List<string>(),
            requestedForActorNumber: originalActor.ActorNumber.Value,
            requestedForActorRole: originalActor.ActorRole.Name,
            energySupplier: energySupplierNumber.Value,
            balanceResponsibleParty: balanceResponsibleParty.Value,
            businessReason: DataHubNames.BusinessReason.BalanceFixing,
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null,
            settlementMethod: SettlementMethod.Flex.Code,
            meteringPointType: MeteringPointType.Consumption.Code);

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var aggregatedMeasureDataRequestAcceptedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow(), new[] { "106", "509", "804" });

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, aggregatedMeasureDataRequestAcceptedMessage);

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
            originalActorPeekResults.Should().HaveCount(3, "because there should be one message for each grid area");
        }

        foreach (var peekResult in originalActorPeekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle!, peekDocumentFormat);

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
                    ReceiverId: originalActor.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: seriesRequest.GridArea,
                    OriginalTransactionIdReference: "123564789123564789123564789123564787",
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

    // [Theory]
    // [MemberData(nameof(DocumentFormatsWithDelegationCombinations))]
    // public async Task AndGiven_OriginalActorRequestsOwnDataWithGridArea_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesOneNotifyAggregatedMeasureDataDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
    // {
    //     /*
    //      *  --- PART 1: Receive request, create process and send message to Wholesale ---
    //      */
    //
    //     // Arrange
    //     var senderSpy = CreateServiceBusSenderSpy();
    //     var originalActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: delegatedFromRole);
    //     var delegatedToActor = (ActorNumber: ActorNumber.Create("2222222222222"), ActorRole: delegatedToRole);
    //     var energySupplierNumber = originalActor.ActorRole == ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("3333333333333");
    //     var chargeOwnerNumber = originalActor.ActorRole != ActorRole.EnergySupplier
    //         ? originalActor.ActorNumber
    //         : ActorNumber.Create("5799999933444");
    //
    //     GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
    //     GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
    //
    //     await GivenDelegation(
    //         new(originalActor.ActorNumber, originalActor.ActorRole),
    //         new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
    //         "512",
    //         ProcessType.RequestWholesaleResults,
    //         GetNow());
    //
    //     // Original actor requests own data
    //     await GivenReceivedAggregatedMeasureDataRequest(
    //         incomingDocumentFormat,
    //         originalActor.ActorNumber,
    //         originalActor.ActorRole,
    //         (2024, 1, 1),
    //         (2024, 1, 31),
    //         "512",
    //         energySupplierNumber,
    //         chargeOwnerNumber,
    //         "25361478",
    //         ChargeType.Tariff,
    //         "123564789123564789123564789123564787",
    //         false);
    //
    //     // Act
    //     await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);
    //
    //     // Assert
    //     var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
    //         senderSpy,
    //         gridAreas: new[] { "512" },
    //         requestedForActorNumber: originalActor.ActorNumber.Value,
    //         requestedForActorRole: originalActor.ActorRole.Name,
    //         energySupplier: energySupplierNumber.Value,
    //         chargeOwnerId: chargeOwnerNumber.Value,
    //         resolution: null,
    //         businessReason: DataHubNames.BusinessReason.WholesaleFixing,
    //         chargeTypes: new List<(string ChargeType, string ChargeCode)>
    //         {
    //             (DataHubNames.ChargeType.Tariff, "25361478"),
    //         },
    //         new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
    //         null);
    //
    //     // TODO: Assert correct process is created?
    //
    //     /*
    //      *  --- PART 2: Receive data from Wholesale and create RSM document ---
    //      */
    //
    //     // Arrange
    //
    //     // Generate a mock WholesaleRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
    //     // It is very important that the generated data is correct,
    //     // since (almost) all assertion after this point is based on this data
    //     var aggregatedMeasureDataRequestAcceptedMessage = AggregatedMeasureDataResponseEventBuilder
    //         .GenerateAggregatedMeasureDataRequestAccepted(message.AggregatedTimeSeriesRequest, GetNow());
    //
    //     await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, aggregatedMeasureDataRequestAcceptedMessage);
    //
    //     // Act
    //     var delegatedActorPeekResults = await WhenActorPeeksAllMessages(
    //         delegatedToActor.ActorNumber,
    //         delegatedToActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     var originalActorPeekResults = await WhenActorPeeksAllMessages(
    //         originalActor.ActorNumber,
    //         originalActor.ActorRole,
    //         peekDocumentFormat);
    //
    //     // Assert
    //     PeekResultDto peekResult;
    //     using (new AssertionScope())
    //     {
    //         delegatedActorPeekResults.Should().BeEmpty("because delegated actor shouldn't receive result when original actor made the request");
    //         peekResult = originalActorPeekResults.Should().ContainSingle("because there should only be one message for one grid area")
    //             .Subject;
    //
    //         peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
    //     }
    //
    //     await ThenNotifyAggregatedMeasureDataDocumentIsCorrect(
    //         peekResult.Bundle,
    //         peekDocumentFormat,
    //         new NotifyAggregatedMeasureDataDocumentAssertionInput(
    //             Timestamp: "2024-07-01T14:57:09Z",
    //             BusinessReasonWithSettlementVersion: new BusinessReasonWithSettlementVersion(
    //                 BusinessReason.WholesaleFixing,
    //                 null),
    //             ReceiverId: originalActor.ActorNumber.Value,
    //             ReceiverRole: originalActor.ActorRole,
    //             SenderId: "5790001330552", // Sender is always DataHub
    //             SenderRole: ActorRole.MeteredDataAdministrator,
    //             ChargeTypeOwner: chargeOwnerNumber.Value,
    //             ChargeCode: "25361478",
    //             ChargeType: ChargeType.Tariff,
    //             Currency: Currency.DanishCrowns,
    //             EnergySupplierNumber: energySupplierNumber.Value,
    //             SettlementMethod: SettlementMethod.Flex,
    //             MeteringPointType: MeteringPointType.Consumption,
    //             GridArea: "512",
    //             OriginalTransactionIdReference: "123564789123564789123564789123564787",
    //             PriceMeasurementUnit: MeasurementUnit.Kwh,
    //             ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
    //             QuantityMeasurementUnit: MeasurementUnit.Kwh,
    //             CalculationVersion: GetNow().ToUnixTimeTicks(),
    //             Resolution: Resolution.Hourly,
    //             Period: new Period(
    //                 CreateDateInstant(2024, 1, 1),
    //                 CreateDateInstant(2024, 1, 31)),
    //             Points: aggregatedMeasureDataRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    // }

    private async Task<string> GetGridAreaFromNotifyAggregatedMeasureDataDocument(Stream documentStream, DocumentFormat documentFormat)
    {
        documentStream.Position = 0;
        if (documentFormat == DocumentFormat.Ebix)
        {
            var ebixAsserter = NotifyAggregatedMeasureDataDocumentAsserter.CreateEbixAsserter(documentStream);
            var gridAreaElement = ebixAsserter.GetElement("PayloadEnergyTimeSeries[1]/MeteringGridAreaUsedDomainLocation/Identification");

            gridAreaElement.Should().NotBeNull("because grid area should be present in the ebIX document");
            gridAreaElement!.Value.Should().NotBeNull("because grid area value should not be null in the ebIX document");
            return gridAreaElement.Value;
        }

        if (documentFormat == DocumentFormat.Xml)
        {
            var cimXmlAsserter = NotifyAggregatedMeasureDataDocumentAsserter.CreateCimXmlAsserter(documentStream);

            var gridAreaCimXmlElement = cimXmlAsserter.GetElement("Series[1]/meteringGridArea_Domain.mRID");

            gridAreaCimXmlElement.Should().NotBeNull("because grid area should be present in the CIM XML document");
            gridAreaCimXmlElement!.Value.Should().NotBeNull("because grid area value should not be null in the CIM XML document");
            return gridAreaCimXmlElement!.Value;
        }

        if (documentFormat == DocumentFormat.Json)
        {
            var cimJsonDocument = await JsonDocument.ParseAsync(documentStream);

            var gridAreaCimJsonElement = cimJsonDocument.RootElement
                .GetProperty("NotifyAggregatedMeasureData_MarketDocument")
                .GetProperty("Series").EnumerateArray().ToList()
                .Single()
                .GetProperty("meteringGridArea_Domain.mRID")
                .GetProperty("value");

            gridAreaCimJsonElement.Should().NotBeNull("because grid area should be present in the CIM JSON document");
            return gridAreaCimJsonElement.GetString()!;
        }

        throw new ArgumentOutOfRangeException(nameof(documentFormat), documentFormat, "Unsupported document format");
    }

    private Task GivenAggregatedMeasureDataRequestAcceptedIsReceived(Guid processId, AggregatedTimeSeriesRequestAccepted acceptedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(AggregatedTimeSeriesRequestAccepted),
            eventPayload: acceptedMessage,
            processId: processId);
    }

    private Task GivenAggregatedMeasureDataRequestRejectedIsReceived(Guid processId, AggregatedTimeSeriesRequestRejected rejectedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(AggregatedTimeSeriesRequestRejected),
            eventPayload: rejectedMessage,
            processId: processId);
    }

    private Task<(AggregatedTimeSeriesRequest AggregatedTimeSeriesRequest, Guid ProcessId)> ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            ServiceBusSenderSpy senderSpy,
            IReadOnlyCollection<string> gridAreas,
            string requestedForActorNumber,
            string requestedForActorRole,
            string? energySupplier,
            string? balanceResponsibleParty,
            string businessReason,
            Period period,
            string? settlementVersion,
            string? settlementMethod,
            string meteringPointType)
    {
        var (message, processId) = AssertServiceBusMessage(
            senderSpy,
            data => AggregatedTimeSeriesRequest.Parser.ParseFrom(data));

        using var assertionScope = new AssertionScope();

        message.GridAreaCodes.Should().BeEquivalentTo(gridAreas);
        message.RequestedForActorNumber.Should().Be(requestedForActorNumber);
        message.RequestedForActorRole.Should().Be(requestedForActorRole);

        if (energySupplier == null)
            message.HasEnergySupplierId.Should().BeFalse();
        else
            message.EnergySupplierId.Should().Be(energySupplier);

        if (balanceResponsibleParty == null)
            message.HasBalanceResponsibleId.Should().BeFalse();
        else
            message.BalanceResponsibleId.Should().Be(balanceResponsibleParty);

        message.BusinessReason.Should().Be(businessReason);

        message.Period.Start.Should().Be(period.Start.ToString());
        message.Period.End.Should().Be(period.End.ToString());

        if (settlementVersion == null)
            message.HasSettlementVersion.Should().BeFalse();
        else
            message.SettlementVersion.Should().Be(settlementVersion);

        if (settlementMethod == null)
            message.HasSettlementMethod.Should().BeFalse();
        else
            message.SettlementMethod.Should().Be(settlementMethod);

        message.MeteringPointType.Should().Be(meteringPointType);

        return Task.FromResult((message, processId));
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML

