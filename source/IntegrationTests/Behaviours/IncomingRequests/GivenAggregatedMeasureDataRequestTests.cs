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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Test class")]
public class GivenAggregatedMeasureDataRequestTests : BehavioursTestBase
{
    public GivenAggregatedMeasureDataRequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static object[][] DocumentFormatsWithActorRoleCombinations()
    {
        // The actor roles who can perform AggregatedMeasureDataRequest's
        var actorRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.MeteredDataResponsible,
            ActorRole.BalanceResponsibleParty,
            ActorRole.GridOperator, // Grid Operator can make requests because of DDM -> MDR hack
        };

        var exceptForIncomingDocumentFormats = new[]
        {
            DocumentFormat.Xml.Name, // TODO: Implement XML request factory
            DocumentFormat.Ebix.Name, // ebIX is not supported for requests
        };

        return DocumentFormats
            .GetAllDocumentFormats(exceptForIncomingDocumentFormats)
            .SelectMany(incomingDocumentFormat => actorRoles
                .SelectMany(actorRole => DocumentFormats.GetAllDocumentFormats(new[]
                    {
                        // TODO: Remove to support XML & ebIX
                        DocumentFormat.Xml.Name,
                        DocumentFormat.Ebix.Name,
                    })
                    .Select(peekDocumentFormat => new object[]
                    {
                        actorRole,
                        incomingDocumentFormat,
                        peekDocumentFormat,
                    })))
            .ToArray();
    }

    [Fact]
    public async Task
        Given_Delegation_When_RequestAggregatedMeasureDataJsonIsReceived_Then_ServiceBusMessageToWholesaleIsAddedToServiceBus()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        GivenNowIs(2024, 7, 1);
        GivenAuthenticatedActorIs(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier);

        await GivenDelegation(
            new(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier),
            new(ActorNumber.Create("1111111111111"), ActorRole.Delegated),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow().Minus(Duration.FromDays(256)),
            GetNow().Plus(Duration.FromDays(256)));

        await GivenReceivedAggregatedMeasureDataRequest(
            documentFormat: DocumentFormat.Json,
            senderActorNumber: ActorNumber.Create("2111111111111"),
            senderActorRole: ActorRole.EnergySupplier,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            periodStart: (2024, 5, 1),
            periodEnd: (2024, 6, 1),
            energySupplier: ActorNumber.Create("2111111111111"),
            balanceResponsibleParty: ActorNumber.Create("3111111111111"),
            series: new (string? GridArea, string TransactionId)[]
            {
                ("512", "123564789123564789123564789123564787"),
            });

        // Act
        await WhenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(senderSpy.Message!);

        // Assert
        senderSpy.MessageSent.Should().BeTrue();
        senderSpy.Message.Should().NotBeNull();
        var serviceBusMessage = senderSpy.Message!;
        serviceBusMessage.Subject.Should().Be(nameof(AggregatedTimeSeriesRequest));
        serviceBusMessage.Body.Should().NotBeNull();

        var aggregatedTimeSeriesRequestMessage = AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);

        aggregatedTimeSeriesRequestMessage.Should().NotBeNull();
        aggregatedTimeSeriesRequestMessage.GridAreaCodes.Should().Equal("512");
        aggregatedTimeSeriesRequestMessage.RequestedForActorNumber.Should().Be("2111111111111");
        aggregatedTimeSeriesRequestMessage.RequestedForActorRole.Should().Be("EnergySupplier");
        aggregatedTimeSeriesRequestMessage.EnergySupplierId.Should().Be("2111111111111");
    }

    [Fact(Skip = "Delegation is not implemented yet")]
    public async Task
        Given_DelegationInTwoGridAreas_When_RequestAggregatedMeasureDataJsonIsReceived_Then_ServiceBusMessageToWholesaleIsAddedToServiceBus()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        GivenNowIs(2024, 7, 1);
        GivenAuthenticatedActorIs(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier);

        await GivenDelegation(
            new(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier),
            new(ActorNumber.Create("1111111111111"), ActorRole.Delegated),
            "512",
            ProcessType.RequestEnergyResults,
            GetNow().Minus(Duration.FromDays(256)),
            GetNow().Plus(Duration.FromDays(256)));

        await GivenDelegation(
            new(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier),
            new(ActorNumber.Create("1111111111111"), ActorRole.Delegated),
            "643",
            ProcessType.RequestEnergyResults,
            GetNow().Minus(Duration.FromDays(256)),
            GetNow().Plus(Duration.FromDays(256)));

        await GivenReceivedAggregatedMeasureDataRequest(
            DocumentFormat.Json,
            senderActorNumber: ActorNumber.Create("2111111111111"),
            senderActorRole: ActorRole.EnergySupplier,
            periodStart: (2024, 5, 1),
            periodEnd: (2024, 6, 1),
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            energySupplier: ActorNumber.Create("2111111111111"),
            balanceResponsibleParty: null,
            series: new (string? GridArea, string TransactionId)[]
            {
                (null, "123564789123564789123564789123564787"),
            });

        // Act
        await WhenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(senderSpy.Message!);

        // Assert
        senderSpy.MessageSent.Should().BeTrue();
        senderSpy.Message.Should().NotBeNull();
        var serviceBusMessage = senderSpy.Message!;
        serviceBusMessage.Subject.Should().Be(nameof(AggregatedTimeSeriesRequest));
        serviceBusMessage.Body.Should().NotBeNull();

        var aggregatedTimeSeriesRequestMessage = AggregatedTimeSeriesRequest.Parser.ParseFrom(serviceBusMessage.Body);

        aggregatedTimeSeriesRequestMessage.Should().NotBeNull();
        aggregatedTimeSeriesRequestMessage.GridAreaCodes.Should().Equal("512", "643");
        aggregatedTimeSeriesRequestMessage.RequestedForActorNumber.Should().Be("2111111111111");
        aggregatedTimeSeriesRequestMessage.RequestedForActorRole.Should().Be("EnergySupplier");
        aggregatedTimeSeriesRequestMessage.EnergySupplierId.Should().Be("2111111111111");
    }

    [Fact]
    public async Task
        Given_AggregatedTimeSeriesRequestAcceptedIsReceived_When_ActorPeeks_Then_CorrectDocumentIsCreated()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        GivenNowIs(2024, 7, 1);
        GivenAuthenticatedActorIs(ActorNumber.Create("2111111111111"), ActorRole.EnergySupplier);
        await GivenGridAreaOwnershipAsync("512", ActorNumber.Create("3111111111111"));

        await GivenReceivedAggregatedMeasureDataRequest(
            DocumentFormat.Json,
            senderActorNumber: ActorNumber.Create("2111111111111"),
            senderActorRole: ActorRole.EnergySupplier,
            periodStart: (2024, 5, 1),
            periodEnd: (2024, 6, 1),
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.Flex,
            energySupplier: ActorNumber.Create("2111111111111"),
            balanceResponsibleParty: null,
            series: new (string? GridArea, string TransactionId)[]
            {
                ("512", "123564789123564789123564789123564787"),
            });

        senderSpy.Message.Should().NotBeNull();
        await GivenInitializeAggregatedMeasureDataProcessDtoIsHandledAsync(senderSpy.Message!);
        await GivenWholesaleAcceptedResponseToAggregatedMeasureDataRequestAsync(senderSpy.Message!);

        // Act
        ClearDbContextCaches();
        var peekedMessage = await WhenPeekMessageAsync(
            MessageCategory.Aggregations,
            ActorNumber.Create("2111111111111"),
            ActorRole.EnergySupplier,
            DocumentFormat.Json);

        // Assert
        peekedMessage.Should().NotBeNull();
        peekedMessage.Bundle.Should().NotBeNull();

        new AssertNotifyAggregatedMeasureDataJsonDocument(peekedMessage.Bundle!)
            .HasEnergySupplierNumber("2111111111111")
            .HasGridAreaCode("512")
            .HasReceiverId("2111111111111")
            .HasOriginalTransactionIdReference("123564789123564789123564789123564787");
    }

    /// <summary>
    /// Even though an actor has delegated his requests to another actor, he should still
    /// be able to request and receive his own data
    /// </summary>
    [Theory]
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinations))]
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

        // Original actor requests own data
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
            series: new (string? GridArea, string TransactionId)[]
            {
                (null, "123564789123564789123564789123564787"),
            });

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.Message!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy,
            gridAreas: new List<string>(),
            requestedForActorNumber: currentActor.ActorNumber.Value,
            requestedForActorRole: currentActor.ActorRole.Name,
            energySupplier: energySupplierNumber.Value,
            balanceResponsibleParty: balanceResponsibleParty.Value,
            businessReason: BusinessReason.BalanceFixing,
            new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
            null,
            settlementMethod: SettlementMethod.Flex,
            meteringPointType: MeteringPointType.Consumption);

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
                    ReceiverId: currentActor.ActorNumber,
                    // ReceiverRole: originalActor.ActorRole,
                    SenderId: ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                    // SenderRole: ActorRole.MeteredDataAdministrator,
                    EnergySupplierNumber: energySupplierNumber,
                    BalanceResponsibleNumber: balanceResponsibleParty,
                    SettlementMethod: SettlementMethod.Flex,
                    MeteringPointType: MeteringPointType.Consumption,
                    GridAreaCode: seriesRequest.GridArea,
                    OriginalTransactionIdReference: "123564789123564789123564789123564787",
                    ProductCode: ProductType.EnergyActive.Code,
                    QuantityMeasurementUnit: MeasurementUnit.Kwh,
                    CalculationVersion: GetNow().ToUnixTimeTicks(),
                    Resolution: Resolution.Hourly,
                    Period: new Period(
                        CreateDateInstant(2024, 1, 1),
                        CreateDateInstant(2024, 1, 31)),
                    Points: seriesRequest.TimeSeriesPoints));
        }

        resultGridAreas.Should().BeEquivalentTo("106", "509");
    }
}
