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

using System.Text;
using Energinet.DataHub.EDI.B2CWebApi.Factories;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Asserts;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenB2CAggregatedMeasureDataRequestTests : AggregatedMeasureDataBehaviourTestBase
{
    public GivenB2CAggregatedMeasureDataRequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static object[][] DocumentFormatsWithMarketRoleCombinations()
    {
        // The actor roles who can perform AggregatedMeasureDataRequest's
        var actorRoles = new List<MarketRole>
        {
            MarketRole.EnergySupplier,
            MarketRole.BalanceResponsibleParty,
            MarketRole.MeteredDataResponsible,
        };

        var peekDocumentFormats = DocumentFormats.GetAllDocumentFormats();

        return actorRoles
                .SelectMany(actorRole => peekDocumentFormats
                    .Select(peekDocumentFormat => new object[]
                    {
                        actorRole,
                        peekDocumentFormat,
                    }))
            .ToArray();
    }

    [Theory]
    [MemberData(nameof(DocumentFormatsWithMarketRoleCombinations))]
    public async Task AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyAggregatedMeasureDataDocumentWithCorrectContent(MarketRole marketRole, DocumentFormat peekDocumentFormat)
    {
        /*
         *  --- PART 1: Receive request, create process and send message to Wholesale ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var actorRole = ActorRole.FromCode(marketRole.Code!);
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: actorRole);
        var energySupplierNumber = currentActor.ActorRole == ActorRole.EnergySupplier
            ? currentActor.ActorNumber
            : ActorNumber.Create("3333333333333");
        var balanceResponsibleParty = currentActor.ActorRole == ActorRole.BalanceResponsibleParty
            ? currentActor.ActorNumber
            : ActorNumber.Create("4444444444444");

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        await GivenReceivedAggregatedMeasureDataRequest(
            senderActorNumber: currentActor.ActorNumber,
            senderActorRole: marketRole,
            energySupplier: energySupplierNumber,
            balanceResponsibleParty: balanceResponsibleParty,
            "512",
            "12356478912356478912356478912356478");

        // Act
        await WhenAggregatedMeasureDataProcessIsInitialized(senderSpy.LatestMessage!);

        // Assert
        var message = await ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect(
            senderSpy: senderSpy,
            new AggregatedTimeSeriesMessageAssertionInput(
                GridAreas: new List<string> { "512" },
                RequestedForActorNumber: currentActor.ActorNumber.Value,
                RequestedForActorRole: currentActor.ActorRole.Name,
                EnergySupplier: energySupplierNumber.Value,
                BalanceResponsibleParty: balanceResponsibleParty.Value,
                BusinessReason: BusinessReason.BalanceFixing,
                new Period(CreateDateInstant(2024, 1, 1), CreateDateInstant(2024, 1, 31)),
                SettlementVersion: null,
                SettlementMethod: null,
                MeteringPointType: MeteringPointType.Production));

        // TODO: Assert correct process is created?

        /*
         *  --- PART 2: Receive data from Wholesale and create RSM document ---
         */

        // Arrange

        // Generate a mock AggregatedTimeSeriesRequestAccepted response from Wholesale, based on the AggregatedMeasureDataRequest
        // It is very important that the generated data is correct,
        // since (almost) all assertion after this point is based on this data
        var acceptedResponse = AggregatedTimeSeriesResponseEventBuilder
            .GenerateAcceptedFrom(message.AggregatedTimeSeriesRequest, GetNow());

        await GivenAggregatedMeasureDataRequestAcceptedIsReceived(message.ProcessId, acceptedResponse);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            currentActor.ActorNumber,
            currentActor.ActorRole,
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
                SenderId: BuildingBlocks.Domain.Models.ActorNumber.Create("5790001330552"),  // Sender is always DataHub
                // SenderRole: ActorRole.MeteredDataAdministrator,
                EnergySupplierNumber: energySupplierNumber,
                BalanceResponsibleNumber: balanceResponsibleParty,
                SettlementMethod: null,
                MeteringPointType: MeteringPointType.Production,
                GridAreaCode: "512",
                OriginalTransactionIdReference: TransactionId.From("12356478912356478912356478912356478"),
                ProductCode: ProductType.EnergyActive.Code,
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Hourly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: TimeSeriesPointAssertionInput.From(acceptedResponse.Series.Single().TimeSeriesPoints)));
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }

    private async Task GivenReceivedAggregatedMeasureDataRequest(
        ActorNumber senderActorNumber,
        MarketRole senderActorRole,
        ActorNumber energySupplier,
        ActorNumber balanceResponsibleParty,
        string gridArea,
        string originalTransactionId)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();
        var dateTimeZone = GetService<DateTimeZone>();
        var systemDateTimeProvider = GetService<ISystemDateTimeProvider>();

        var request = new RequestAggregatedMeasureDataMarketRequest(
            CalculationType: CalculationType.BalanceFixing,
            MeteringPointType: Energinet.DataHub.EDI.B2CWebApi.Models.MeteringPointType.Production,
            "2024-01-01T22:00:00Z",
            "2024-01-31T22:00:00Z",
            gridArea,
            energySupplier.Value,
            balanceResponsibleParty.Value);

        var requestMessage =
            RequestAggregatedMeasureDataDtoFactory.Create(
                request,
                senderActorNumber.Value,
                senderActorRole.Name,
                dateTimeZone,
                systemDateTimeProvider.Now(),
                originalTransactionId);

        var incomingMessageStream = GenerateStreamFromString(new Serializer().Serialize(requestMessage));

        var response = await
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                DocumentFormat.Json,
                IncomingDocumentType.B2CRequestAggregatedMeasureData,
                DocumentFormat.Json,
                CancellationToken.None);

        using var scope = new AssertionScope();
        response.IsErrorResponse.Should().BeFalse();
        response.MessageBody.Should().BeEmpty();
    }
}
