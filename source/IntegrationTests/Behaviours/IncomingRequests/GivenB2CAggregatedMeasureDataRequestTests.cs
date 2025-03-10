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
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenB2CAggregatedMeasureDataRequestTests : AggregatedMeasureDataBehaviourTestBase
{
    public GivenB2CAggregatedMeasureDataRequestTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task When_Received_Then_ProcessManagerIsCorrectlyInformed()
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var testDataDescription = GivenDatabricksResultDataForEnergyResultPerEnergySupplier();
        var testMessageData = testDataDescription.ExampleEnergySupplier;

        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = testMessageData.ExampleMessageData.EnergySupplier;
        var balanceResponsibleParty = testMessageData.ExampleMessageData.BalanceResponsible;
        var actor = (ActorNumber: testMessageData.ActorNumber, ActorRole: ActorRole.EnergySupplier);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var gridAreaWithNoData = "000";

        // Act
        await GivenReceivedAggregatedMeasureDataRequest(
            transactionId,
            actor.ActorNumber,
            actor.ActorRole,
            energySupplierNumber!,
            balanceResponsibleParty!,
            gridAreaWithNoData);

        // Assert
        ThenRequestCalculatedEnergyTimeSeriesInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedEnergyTimeSeriesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.BalanceFixing,
                PeriodStart: CreateDateInstant(2022, 1, 1),
                PeriodEnd: CreateDateInstant(2022, 2, 1),
                energySupplierNumber!.Value,
                balanceResponsibleParty!.Value,
                new List<string> { gridAreaWithNoData },
                SettlementMethod: SettlementMethod.NonProfiled,
                MeteringPointType: testMessageData.ExampleMessageData.MeteringPointType,
                SettlementVersion: null));
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }

    private async Task GivenReceivedAggregatedMeasureDataRequest(
        TransactionId transactionId,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        ActorNumber energySupplier,
        ActorNumber balanceResponsibleParty,
        string gridArea)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();
        var dateTimeZone = GetService<DateTimeZone>();
        var clock = GetService<IClock>();

        var request = new RequestAggregatedMeasureDataMarketRequest(
            CalculationType: CalculationType.BalanceFixing,
            MeteringPointType: Energinet.DataHub.EDI.B2CWebApi.Models.MeteringPointType.NonProfiledConsumption,
            "2022-01-01T22:00:00Z",
            "2022-02-01T22:00:00Z",
            gridArea,
            energySupplier.Value,
            balanceResponsibleParty.Value);

        var requestMessage =
            RequestAggregatedMeasureDataDtoFactory.Create(
                transactionId,
                request,
                senderActorNumber.Value,
                senderActorRole.Name,
                dateTimeZone,
                clock.GetCurrentInstant());

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
