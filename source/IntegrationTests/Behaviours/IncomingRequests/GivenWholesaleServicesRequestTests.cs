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
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.Edi.Responses;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenWholesaleServicesRequestTests : WholesaleServicesBehaviourTestBase
{
    public GivenWholesaleServicesRequestTests(IntegrationTestFixture fixture, ITestOutputHelper testOutput)
        : base(fixture, testOutput)
    {
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

    [Theory]
    [MemberData(nameof(DocumentFormatsWithAllActorRoleCombinations))]
    public async Task
        AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(
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
            chargeCode: "25361478",
            chargeType: ChargeType.Tariff,
            isMonthly: false,
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
                null,
                BusinessReason.WholesaleFixing.Name,
                new List<(string ChargeType, string? ChargeCode)> { (ChargeType.Tariff.Name, "25361478"), },
                new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                null));

        // TODO: Assert correct process is created?

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
                ChargeCode: "25361478",
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "512",
                TransactionId.From("12356478912356478912356478912356478"),
                PriceMeasurementUnit: MeasurementUnit.KilowattHour,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: acceptedResponse.Series.Single().TimeSeriesPoints));
    }

    [Fact]
    public async Task AndGiven_TooLongChargeCode_When_WholesaleServicesProcessIsInitialized_Then_DbExceptionIsThrown()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
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
            DocumentFormat.Json,
            actor.ActorNumber,
            actor.ActorRole,
            (2024, 1, 1),
            (2024, 1, 31),
            energySupplierNumber,
            chargeOwnerNumber,
            "64852f7a-b928-477e-9213-e219bf250ec3-that-is-too-long",
            ChargeType.Tariff,
            false,
            [
                ("512", TransactionId.From("12356478912356478912356478912356478")),
            ]);

        // Act
        var initializeProcess = async () => await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        await initializeProcess
            .Should()
            .ThrowExactlyAsync<DbUpdateException>()
            .WithInnerException<DbUpdateException, SqlException>()
            .WithMessage(
                "String or binary data would be truncated *WholesaleServicesProcessChargeTypes', column 'Id'. Truncated value: '64852f7a-b928-477e-9213-e219bf250ec3-'*");
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinationsForNullGridArea))]
    public async Task AndGiven_DataInTwoGridAreas_When_ActorPeeksAllMessages_Then_ReceivesTwoNotifyWholesaleServicesDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat incomingDocumentFormat, DocumentFormat peekDocumentFormat)
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
        await GivenGridAreaOwnershipAsync("106", gridOperatorNumber);
        await GivenGridAreaOwnershipAsync("509", gridOperatorNumber);

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
                (null, TransactionId.From("12356478912356478912356478912356478")),
            });

        // Act
        await WhenWholesaleServicesProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenWholesaleServicesRequestServiceBusMessageIsCorrect(
            senderSpy,
            new WholesaleServicesMessageAssertionInput(
                GridAreas: Array.Empty<string>(),
                RequestedForActorNumber: actor.ActorNumber.Value,
                RequestedForActorRole: actor.ActorRole.Name,
                EnergySupplierId: energySupplierNumber.Value,
                ChargeOwnerId: chargeOwnerNumber.Value,
                Resolution: null,
                BusinessReason: BusinessReason.WholesaleFixing.Name,
                ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                {
                    (ChargeType.Tariff.Name, "25361478"),
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

        // Generate a mock WholesaleServicesRequestAccepted response from Wholesale, based on the WholesaleServicesRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var generateDataInGridAreas = new List<string> { "106", "509" };
        var wholesaleAcceptedResponse = WholesaleServicesResponseEventBuilder
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow(), null, null, generateDataInGridAreas);

        await GivenWholesaleServicesRequestAcceptedIsReceived(message.ProcessId, wholesaleAcceptedResponse);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            actor.ActorNumber,
            actor.ActorRole,
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
            var peekResultGridArea = await GetGridAreaFromNotifyWholesaleServicesDocument(peekResult.Bundle, peekDocumentFormat);

            resultGridAreas.Add(peekResultGridArea);

            var seriesRequest = wholesaleAcceptedResponse.Series
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
                    TransactionId.From("12356478912356478912356478912356478"),
                    PriceMeasurementUnit: MeasurementUnit.KilowattHour,
                    ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                    QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: seriesRequest.TimeSeriesPoints));
        }

        resultGridAreas.Should().BeEquivalentTo("106", "509");
    }

    [Theory]
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
                BusinessReason: BusinessReason.WholesaleFixing.Name,
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
                PriceMeasurementUnit: MeasurementUnit.KilowattHour,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: acceptedResponse.Series.Single().TimeSeriesPoints));
    }

    [Theory]
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
                    BusinessReason: BusinessReason.WholesaleFixing.Name,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (ChargeType.Tariff.Name, "25361478"),
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
                    BusinessReason: BusinessReason.WholesaleFixing.Name,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (ChargeType.Tariff.Name, "25361478"),
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
                    BusinessReason: BusinessReason.WholesaleFixing.Name,
                    ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                    {
                        (ChargeType.Tariff.Name, "25361478"),
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
                    ProcessId: message.ProcessId,
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
                    PriceMeasurementUnit: MeasurementUnit.KilowattHour,
                    ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                    QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
        // Arrange
        var incomingDocumentFormat = DocumentFormat.FromName(incomingDocumentFormatName);
        var senderSpy = CreateServiceBusSenderSpy();
        var actor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.EnergySupplier);
        var energySupplierNumber = ActorNumber.Create("3333333333333");
        var chargeOwnerNumber = ActorNumber.Create("5799999933444");

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
                ("512", TransactionId.From("123564789123564789123564789123564787")),
            });

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
                null,
                BusinessReason.WholesaleFixing.Name,
                new List<(string ChargeType, string? ChargeCode)> { (ChargeType.Tariff.Name, "25361478"), },
                new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                null));

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

        var expectedReasonMessage = "Elleverandør i header og payload stemmer ikke overens / "
                                    + "Energysupplier in header and payload must be the same";

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
                TransactionId.From("123564789123564789123564789123564787"),
                "E16",
                expectedReasonMessage));
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
                BusinessReason.WholesaleFixing.Name,
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
            PriceMeasurementUnit: MeasurementUnit.KilowattHour,
            ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
            QuantityMeasurementUnit: MeasurementUnit.KilowattHour,
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
