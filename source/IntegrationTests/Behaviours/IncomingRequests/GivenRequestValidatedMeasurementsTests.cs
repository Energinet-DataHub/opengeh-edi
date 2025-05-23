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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.ProcessManager.Abstractions.Client;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using Period = NodaTime.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenRequestValidatedMeasurementsTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : RequestValidatedMeasurementsBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> SupportedDocumentFormats =>
    [
        DocumentFormat.Json,
        // DocumentFormat.Xml,
        // DocumentFormat.Ebix
    ];

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        When_ActorPeeksMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var senderActor = new Actor(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier);

        var now = new LocalDate(2025, 5, 23);
        var periodEnd = CreateDateInstant(now.Year, now.Month, now.Day);
        var oneYearAgo = now.Minus(Period.FromYears(1));
        var periodStart = CreateDateInstant(oneYearAgo.Year, oneYearAgo.Month, oneYearAgo.Day);

        GivenNowIs(periodEnd);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestValidatedMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            [
                (TransactionId: transactionId,
                    PeriodStart: periodStart,
                    PeriodEnd: periodEnd,
                    MeteringPointId: meteringPointId),
            ]);

        // Assert
        var message = ThenRequestValidatedMeasurementsInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            documentFormat,
            new RequestValidatedMeasurementsInputV1AssertionInput(
                RequestedForActor: senderActor,
                BusinessReason: BusinessReason.PeriodicMetering,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                StartDateTime: periodStart,
                EndDateTime: periodEnd));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // TODO: Implement this part when the Process Manager is ready to send data to the EDI
    }
}
