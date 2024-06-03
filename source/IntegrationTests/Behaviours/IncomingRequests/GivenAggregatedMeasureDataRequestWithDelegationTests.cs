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
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;
// ReSharper disable InconsistentNaming

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

#pragma warning disable CS1570 // XML comment has badly formed XML
[SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Test class")]
[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test class")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test class")]
public class GivenAggregatedMeasureDataRequestWithDelegationTests : AggregatedMeasureDataBehaviourTestBase
{
    public GivenAggregatedMeasureDataRequestWithDelegationTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static object[][] DocumentFormatsWithAllRoleCombinations() => DocumentFormatsWithRoleCombinations(false);

    public static object[][] DocumentFormatsWithRoleCombinationsForNullGridArea() => DocumentFormatsWithRoleCombinations(true);

    public static object[][] DocumentFormatsWithRoleCombinations(bool nullGridArea)
    {
        var roleCombinations = new List<(ActorRole DelegatedFrom, ActorRole DelegatedTo)>
        {
            // Energy supplier, metered data responsible and balance responsible can only delegate to delegated
            (ActorRole.EnergySupplier, ActorRole.Delegated),
            (ActorRole.BalanceResponsibleParty, ActorRole.Delegated),
        };

        // Grid operator and MDR cannot make request with null grid area
        if (!nullGridArea)
        {
            roleCombinations.Add((ActorRole.MeteredDataResponsible, ActorRole.Delegated));
            roleCombinations.Add((ActorRole.GridOperator, ActorRole.Delegated));

            // Grid operator can delegate to both delegated and grid operator
            roleCombinations.Add((ActorRole.GridOperator, ActorRole.GridOperator));
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

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
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
        var gridAreaOwner = originalActor.ActorRole == ActorRole.GridOperator
            || originalActor.ActorRole == ActorRole.MeteredDataResponsible
            ? originalActor.ActorNumber
            : ActorNumber.Create("5555555555555");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync("512", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("804", gridAreaOwner); // No delegation/request for 804, so shouldn't be used
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow());

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>() { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
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
                OriginalTransactionIdReference: TransactionId.From("123564789123564789123564789123564787"),
                ProductCode: "8716867000030", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: aggregatedMeasureDataRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    }

    /// <summary>
    /// Rejected document based on example:
    ///     https://energinet.sharepoint.com/:u:/r/sites/DH3ART-team/Delte%20dokumenter/General/CIM/CIM%20XSD%20-%20XML/XML%20filer/XML%2020220706%20-%20Danske%20koder%20-%20v.1.5/Reject%20request%20aggregated%20measure%20data.xml?csf=1&web=1&e=F5bcPI
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_AndGiven_InvalidRequest_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectAggregatedMeasureDataDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var gridAreaOwner = originalActor.ActorRole == ActorRole.GridOperator
                            || originalActor.ActorRole == ActorRole.MeteredDataResponsible
            ? originalActor.ActorNumber
            : ActorNumber.Create("5555555555555");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync("512", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("804", gridAreaOwner); // No delegation for 804, so shouldn't be used
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow());

        // Setup fake request (period end is before period start)
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 01, 01),
            periodEnd: (2023, 12, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>() { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplier: energySupplierNumber.Value,
                BalanceResponsibleParty: balanceResponsibleParty.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2023, 12, 31)),
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
        var rejectedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateRejectedFrom(message.AggregatedTimeSeriesRequest);

        await GivenAggregatedMeasureDataRequestRejectedIsReceived(message.ProcessId, rejectedMessage);

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
                                    + " med en balancefiksering eller korrektioner / It is only possible to request"
                                    + " data for a full month in relation to balancefixing or corrections";

        await ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new(
                BusinessReason.BalanceFixing,
                "5790001330552",
                delegatedToActor.ActorNumber.Value,
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                ReasonCode.FullyRejected.Code,
                TransactionId.From("123564789123564789123564789123564787"),
                "E17",
                expectedReasonMessage));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllRoleCombinations), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_AndGiven_OriginalActorRequestsOwnData_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesOneNotifyAggregatedMeasureDataDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var gridAreaOwner = originalActor.ActorRole == ActorRole.GridOperator
                            || originalActor.ActorRole == ActorRole.MeteredDataResponsible
            ? originalActor.ActorNumber
            : ActorNumber.Create("5555555555555");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync("512", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("804", gridAreaOwner); // No delegation for 804, so shouldn't be used
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow());

        // Original actor requests own data
        await GivenReceivedAggregatedMeasureDataRequest(
            incomingDocumentFormat,
            originalActor.ActorNumber,
            originalActor.ActorRole,
            MeteringPointType.Consumption,
            SettlementMethod.Flex,
            (2024, 1, 1),
            (2024, 1, 31),
            energySupplierNumber,
            balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>() { "512" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
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
        var aggregatedMeasureDataRequestAcceptedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow());

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
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            delegatedActorPeekResults.Should().BeEmpty("because delegated actor shouldn't receive result when original actor made the request");
            peekResult = originalActorPeekResults.Should().ContainSingle("because there should only be one message for one grid area")
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
                ReceiverId: originalActor.ActorNumber,
                //ReceiverRole: originalActor.ActorRole,
                SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                //SenderRole: ActorRole.MeteredDataAdministrator,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridAreaCode: "512",
                OriginalTransactionIdReference: TransactionId.From("123564789123564789123564789123564787"),
                ProductCode: "8716867000030", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: aggregatedMeasureDataRequestAcceptedMessage.Series.Single().TimeSeriesPoints));
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
    public async Task AndGiven_DelegationInTwoGridAreas_When_DelegatedActorPeeksAllMessages_Then_ReceivesTwoNotifyAggregatedMeasureDataDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var gridAreaOwner = originalActor.ActorRole == ActorRole.GridOperator
                            || originalActor.ActorRole == ActorRole.MeteredDataResponsible
            ? originalActor.ActorNumber
            : ActorNumber.Create("5555555555555");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync("512", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("609", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("804", gridAreaOwner); // No delegation for 804, so shouldn't be used
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow());

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestEnergyResults,
            GetNow());

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 1, 1),
            periodEnd: (2024, 1, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>() { "512", "609" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplier: energySupplierNumber.Value,
                BalanceResponsibleParty: balanceResponsibleParty.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption));

        // TODO: Assert correct process is created

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock AggregatedTimeSeriesRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
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
        using (new AssertionScope())
        {
            originalActorPeekResults.Should().BeEmpty("because original actor shouldn't receive result when delegated actor made the request");
            delegatedActorPeekResults.Should().HaveCount(2, "because there should be one message for each grid area");
        }

        var resultGridAreas = new List<string>();
        foreach (var peekResult in delegatedActorPeekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle!, peekDocumentFormat);

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
                    ReceiverId: delegatedToActor.ActorNumber,
                    //ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"), // Sender is always DataHub
                    //SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: seriesRequest.GridArea,
                    OriginalTransactionIdReference: TransactionId.From("123564789123564789123564789123564787"),
                    ProductCode: "8716867000030", // Example says "8716867000030", but document writes as "5790001330590"?
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
    ///     https://energinet.sharepoint.com/:u:/r/sites/DH3ART-team/Delte%20dokumenter/General/CIM/CIM%20XSD%20-%20XML/XML%20filer/XML%2020220706%20-%20Danske%20koder%20-%20v.1.5/Reject%20request%20aggregated%20measure%20data.xml?csf=1&web=1&e=F5bcPI
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
    public async Task AndGiven_DelegationInTwoGridAreas_AndGiven_InvalidRequest_When_DelegatedActorPeeksAllMessages_Then_ReceivesOneRejectAggregatedMeasureDataDocumentsWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        var gridAreaOwner = originalActor.ActorRole == ActorRole.GridOperator
                            || originalActor.ActorRole == ActorRole.MeteredDataResponsible
            ? originalActor.ActorNumber
            : ActorNumber.Create("5555555555555");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        await GivenGridAreaOwnershipAsync("512", gridAreaOwner);
        await GivenGridAreaOwnershipAsync("804", gridAreaOwner); // No delegation for 804, so shouldn't be used
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow());

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "609",
            ProcessType.RequestEnergyResults,
            GetNow());

        // Setup fake request (period end is before period start)
        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: incomingDocumentFormat,
            senderActorNumber: delegatedToActor.ActorNumber,
            senderActorRole: originalActor.ActorRole,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 01, 01),
            periodEnd: (2023, 12, 31),
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>() { "512", "609" },
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
                EnergySupplier: energySupplierNumber.Value,
                BalanceResponsibleParty: balanceResponsibleParty.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                Period: new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2023, 12, 31)),
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
        var rejectedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateRejectedFrom(message.AggregatedTimeSeriesRequest);

        await GivenAggregatedMeasureDataRequestRejectedIsReceived(message.ProcessId, rejectedMessage);

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
                                    + " med en balancefiksering eller korrektioner / It is only possible to request"
                                    + " data for a full month in relation to balancefixing or corrections";

        await ThenRejectRequestAggregatedMeasureDataDocumentIsCorrect(
            peekResult.Bundle,
            peekDocumentFormat,
            new(
                BusinessReason.BalanceFixing,
                "5790001330552",
                delegatedToActor.ActorNumber.Value,
                InstantPattern.General.Parse("2024-07-01T14:57:09Z").Value,
                ReasonCode.FullyRejected.Code,
                TransactionId.From("123564789123564789123564789123564787"),
                "E17",
                expectedReasonMessage));
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithRoleCombinationsForNullGridArea), MemberType = typeof(GivenAggregatedMeasureDataRequestWithDelegationTests))]
    public async Task AndGiven_DelegationInOneGridArea_AndGiven_OriginalActorRequestsOwnDataWithDataInThreeGridAreas_When_OriginalActorPeeksAllMessages_Then_OriginalActorReceivesThreeNotifyAggregatedMeasureDataDocumentWithCorrectContent(DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat, ActorRole delegatedFromRole, ActorRole delegatedToRole)
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
        GivenAuthenticatedActorIs(originalActor.ActorNumber, originalActor.ActorRole);

        await GivenDelegation(
            new(originalActor.ActorNumber, originalActor.ActorRole),
            new(delegatedToActor.ActorNumber, delegatedToActor.ActorRole),
            "512",
            ProcessType.RequestEnergyResults,
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
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            new (string? GridArea, TransactionId TransactionId)[]
            {
                (null, TransactionId.From("123564789123564789123564789123564787")),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string>(),
                RequestedForActorNumber: originalActor.ActorNumber.Value,
                RequestedForActorRole: originalActor.ActorRole.Name,
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
        var aggregatedMeasureDataRequestAcceptedMessage = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow(), new[] { "106", "512", "804" });

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

        var resultGridAreas = new List<string>();
        foreach (var peekResult in originalActorPeekResults)
        {
            peekResult.Bundle.Should().NotBeNull("because peek result should contain a document stream");
            var peekResultGridArea = await GetGridAreaFromNotifyAggregatedMeasureDataDocument(peekResult.Bundle!, peekDocumentFormat);

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
                    ReceiverId: originalActor.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: seriesRequest.GridArea,
                    OriginalTransactionIdReference: TransactionId.From("123564789123564789123564789123564787"),
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: seriesRequest.TimeSeriesPoints));
        }

        resultGridAreas.Should().BeEquivalentTo("106", "512", "804");
    }

    [Theory]
    [InlineData("Xml", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.GridOperator, DataHubNames.ActorRole.Delegated)]
    [InlineData("Xml", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.EnergySupplier, DataHubNames.ActorRole.Delegated)]
    [InlineData("Xml", DataHubNames.ActorRole.BalanceResponsibleParty, DataHubNames.ActorRole.Delegated)]
    [InlineData("Json", DataHubNames.ActorRole.BalanceResponsibleParty, DataHubNames.ActorRole.Delegated)]
    public async Task AndGiven_RequestDoesNotContainOriginalActorNumber_When_DelegatedActorPeeksAllMessages_Then_DelegationIsUnsuccessfulSoRequestIsRejectedWithCorrectInvalidRoleError(string incomingDocumentFormatName, string originalActorRoleName, string delegatedToRoleName)
    {
        var incomingDocumentFormat = DocumentFormat.FromName(incomingDocumentFormatName);
        var originalActorRole = ActorRole.FromName(originalActorRoleName);
        var delegatedToRole = ActorRole.FromName(delegatedToRoleName);

        var senderSpy = CreateServiceBusSenderSpy();
        var originalActor = new Actor(ActorNumber.Create("1111111111111"), actorRole: originalActorRole);
        var delegatedToActor = new Actor(actorNumber: ActorNumber.Create("2222222222222"), actorRole: delegatedToRole);

        if (originalActor.ActorRole == ActorRole.GridOperator)
            await GivenGridAreaOwnershipAsync("804", originalActor.ActorNumber);

        GivenAuthenticatedActorIs(delegatedToActor.ActorNumber, delegatedToActor.ActorRole);
        await GivenDelegation(
            originalActor,
            delegatedToActor,
            "804",
            ProcessType.RequestEnergyResults,
            GetNow());

        var response = await GivenReceivedAggregatedMeasureDataRequest(
            incomingDocumentFormat,
            delegatedToActor.ActorNumber,
            originalActor.ActorRole,
            MeteringPointType.Consumption,
            SettlementMethod.Flex,
            (2024, 1, 1),
            (2023, 12, 31),
            null,
            null,
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

    private Task GivenAggregatedMeasureDataRequestRejectedIsReceived(Guid processId, AggregatedTimeSeriesRequestRejected rejectedMessage)
    {
        return HavingReceivedInboxEventAsync(
            eventType: nameof(AggregatedTimeSeriesRequestRejected),
            eventPayload: rejectedMessage,
            processId: processId);
    }
}

#pragma warning restore CS1570 // XML comment has badly formed XML

