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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenB2CWholesaleServicesRequestTests : WholesaleServicesBehaviourTestBase
{
    public GivenB2CWholesaleServicesRequestTests(IntegrationTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    public static object[][] DocumentFormatsWithActorRoleCombinations()
    {
        // The actor roles who can perform WholesaleServicesRequest's
        var actorRoles = new List<ActorRole>
        {
            ActorRole.EnergySupplier,
            ActorRole.SystemOperator,
            ActorRole.GridAccessProvider,
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
    [MemberData(nameof(DocumentFormatsWithActorRoleCombinations))]
    public async Task AndGiven_DataInOneGridArea_When_ActorPeeksAllMessages_Then_ReceivesOneNotifyWholesaleServicesDocumentWithCorrectContent(ActorRole actorRole, DocumentFormat peekDocumentFormat)
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

        GivenAuthenticatedActorIs(actor.ActorNumber, actorRole);
        await GivenGridAreaOwnershipAsync("512", gridOperatorNumber);

        var transactionId = await GivenReceivedB2CWholesaleServicesRequest(
            senderActorNumber: actor.ActorNumber,
            senderActorRole: actorRole,
            energySupplier: energySupplierNumber,
            gridArea: "512");

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
                null,
                Resolution.Monthly.Name,
                DataHubNames.BusinessReason.WholesaleFixing,
                ChargeTypes: new List<(string ChargeType, string? ChargeCode)>
                {
                    (DataHubNames.ChargeType.Tariff, null),
                },
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
            .GenerateAcceptedFrom(message.WholesaleServicesRequest, GetNow(), chargeOwnerNumber.Value);

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
                SenderId: "5790001330552",  // Sender is always DataHub
                SenderRole: ActorRole.MeteredDataAdministrator,
                ChargeTypeOwner: chargeOwnerNumber.Value,
                ChargeCode: null,
                ChargeType: ChargeType.Tariff,
                Currency: Currency.DanishCrowns,
                EnergySupplierNumber: energySupplierNumber.Value,
                SettlementMethod: SettlementMethod.Flex,
                MeteringPointType: MeteringPointType.Consumption,
                GridArea: "512",
                OriginalTransactionIdReference: TransactionId.From(transactionId),
                PriceMeasurementUnit: MeasurementUnit.Kwh,
                ProductCode: "5790001330590", // Example says "8716867000030", but document writes as "5790001330590"?
                QuantityMeasurementUnit: MeasurementUnit.Kwh,
                CalculationVersion: GetNow().ToUnixTimeTicks(),
                Resolution: Resolution.Monthly,
                Period: new Period(
                    CreateDateInstant(2024, 1, 1),
                    CreateDateInstant(2024, 1, 31)),
                Points: acceptedResponse.Series.Single().TimeSeriesPoints));
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }

    private async Task<string> GivenReceivedB2CWholesaleServicesRequest(
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        ActorNumber? energySupplier,
        string gridArea,
        bool assertRequestWasSuccessful = true)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var b2CRequest = new RequestWholesaleSettlementMarketRequest(
            CalculationType.WholesaleFixing,
            "2024-01-01T22:00:00Z",
            "2024-01-31T22:00:00Z",
            gridArea,
            energySupplier?.Value,
            Resolution.Monthly.Name,
            PriceType.MonthlyTariff);

        var requestMessage = RequestWholesaleSettlementDtoFactory.Create(
            b2CRequest,
            senderActorNumber.Value,
            senderActorRole.Name,
            DateTimeZoneProviders.Tzdb["Europe/Copenhagen"],
            DateTime.Now.ToUniversalTime().ToInstant());

        var incomingMessageStream = GenerateStreamFromString(new Serializer().Serialize(requestMessage));

        var response = await
            incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                incomingMessageStream,
                DocumentFormat.Json,
                IncomingDocumentType.B2CRequestWholesaleSettlement,
                DocumentFormat.Json,
                CancellationToken.None);

        if (assertRequestWasSuccessful)
        {
            using var scope = new AssertionScope();
            response.IsErrorResponse.Should().BeFalse();
            response.MessageBody.Should().BeEmpty();
        }

        return requestMessage.Series.First().Id;
    }
}
