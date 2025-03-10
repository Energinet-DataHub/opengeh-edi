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
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;
using ActorRole = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.ActorRole;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenB2CWholesaleServicesRequestTests : WholesaleServicesBehaviourTestBase
{
    public GivenB2CWholesaleServicesRequestTests(IntegrationTestFixture fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    [Fact]
    public async Task When_Received_Then_ProcessManagerIsCorrectlyInformed()
    {
        // Arrange
        var testDataDescription = GivenDatabricksResultDataForWholesaleResultAmountPerCharge();
        var exampleWholesaleResultMessageForActor = testDataDescription.ExampleWholesaleResultMessageData;
        var actorRole = ActorRole.EnergySupplier;
        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var energySupplierNumber = ActorNumber.Create("5790001662233");
        var chargeOwnerNumber = actorRole == ActorRole.SystemOperator ? ActorNumber.Create(DataHubDetails.SystemOperatorActorNumber.Value) : ActorNumber.Create("8500000000502");
        var gridOperatorNumber = ActorNumber.Create("4444444444444");
        var actor = (ActorNumber: actorRole == ActorRole.EnergySupplier
            ? energySupplierNumber
            : actorRole == ActorRole.SystemOperator
                ? chargeOwnerNumber
                : gridOperatorNumber, ActorRole: actorRole);
        var transactionId = TransactionId.From("12356478912356478912356478912356478");

        var expectedGridArea = exampleWholesaleResultMessageForActor.GridArea;
        var expectedChargeType = exampleWholesaleResultMessageForActor.ChargeType!;

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(actor.ActorNumber, actor.ActorRole);
        await GivenGridAreaOwnershipAsync(expectedGridArea, gridOperatorNumber);

        // Act
        await GivenReceivedB2CWholesaleServicesRequest(transactionId, actor.ActorNumber, actor.ActorRole, energySupplierNumber, expectedGridArea);

        // Assert
        ThenRequestCalculatedWholesaleServicesCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestCalculatedWholesaleServicesInputV1AssertionInput(
                transactionId,
                actor.ActorNumber.Value,
                actor.ActorRole.Name,
                BusinessReason.WholesaleFixing,
                Resolution: Resolution.Monthly,
                PeriodStart: CreateDateInstant(2023, 2, 1),
                PeriodEnd: CreateDateInstant(2023, 3, 1),
                energySupplierNumber.Value,
                null,
                new List<string> { expectedGridArea },
                null,
                new List<ChargeTypeInput> { new(expectedChargeType.Name, null) }));
    }

    private static IncomingMarketMessageStream GenerateStreamFromString(string jsonString)
    {
        var encoding = Encoding.UTF8;
        var byteArray = encoding.GetBytes(jsonString);
        var memoryStream = new MemoryStream(byteArray);
        return new IncomingMarketMessageStream(memoryStream);
    }

    private async Task GivenReceivedB2CWholesaleServicesRequest(
        TransactionId transactionId,
        ActorNumber senderActorNumber,
        ActorRole senderActorRole,
        ActorNumber? energySupplier,
        string gridArea,
        bool assertRequestWasSuccessful = true)
    {
        var incomingMessageClient = GetService<IIncomingMessageClient>();

        var b2CRequest = new RequestWholesaleSettlementMarketRequest(
            CalculationType.WholesaleFixing,
            "2023-02-01T22:00:00Z",
            "2023-03-01T22:00:00Z",
            gridArea,
            energySupplier?.Value,
            Resolution.Monthly.Name,
            PriceType.MonthlySubscription);

        var requestMessage = RequestWholesaleSettlementDtoFactory.Create(
            transactionId,
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
    }
}
