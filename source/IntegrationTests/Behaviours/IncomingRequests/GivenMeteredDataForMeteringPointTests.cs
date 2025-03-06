﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;
using FluentAssertions;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public sealed class GivenMeteredDataForMeteringPointTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : MeteredDataForMeteringPointBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> SupportedDocumentFormats =>
    [
        DocumentFormat.Json,
        DocumentFormat.Xml,
    ];

    [Theory(Skip = "Service bus is not used for metered data")]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task When_ActorPeeksAllMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.GridAccessProvider);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        var transactionIdPrefix = Guid.NewGuid().ToString("N");

        var transactionId1 = $"{transactionIdPrefix}-1";
        var transactionId2 = $"{transactionIdPrefix}-2";

        await GivenReceivedMeteredDataForMeteringPoint(
            documentFormat: documentFormat,
            senderActorNumber: currentActor.ActorNumber,
            [
                (transactionId1,
                    InstantPattern.General.Parse("2024-11-28T13:51:42Z").Value,
                    InstantPattern.General.Parse("2024-11-29T09:15:28Z").Value,
                    Resolution.Hourly),
                (transactionId2,
                    InstantPattern.General.Parse("2024-11-24T18:51:58Z").Value,
                    InstantPattern.General.Parse("2024-11-25T03:39:45Z").Value,
                    Resolution.QuarterHourly),
            ]);

        await WhenMeteredDataForMeteringPointProcessIsInitialized(senderSpy.LatestMessage!);

        // ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            ActorNumber.Create("8100000000115"),
            ActorRole.EnergySupplier,
            documentFormat);

        // Assert
        peekResults.Should().HaveCount(2);

        foreach (var peekResultDto in peekResults)
        {
            // This is not pretty, but it works for now
            var foo = new StreamReader(peekResultDto.Bundle);
            var content = await foo.ReadToEndAsync();
            var isTransOne = content.Contains(transactionId1);
            peekResultDto.Bundle.Position = 0;

            await ThenNotifyValidatedMeasureDataDocumentIsCorrect(
                peekResultDto.Bundle,
                documentFormat,
                new NotifyValidatedMeasureDataDocumentAssertionInput(
                    new RequiredHeaderDocumentFields(
                        "E23",
                        "8100000000115",
                        "A10",
                        "5790001330552",
                        "A10",
                        "DGL",
                        "DDQ",
                        "2024-07-01T14:57:09Z"),
                    new OptionalHeaderDocumentFields(
                        "23",
                        [
                            new AssertSeriesDocumentFieldsInput(
                                1,
                                new RequiredSeriesFields(
                                    TransactionId.From(
                                        string.Join(
                                            string.Empty,
                                            isTransOne ? transactionId1.Reverse() : transactionId2.Reverse())),
                                    "579999993331812345",
                                    "A10",
                                    MeteringPointType.FromCode("E17"),
                                    "KWH",
                                    new RequiredPeriodDocumentFields(
                                        isTransOne ? "PT1H" : "PT15M",
                                        isTransOne ? "2024-11-28T13:51Z" : "2024-11-24T18:51Z",
                                        isTransOne ? "2024-11-29T09:15Z" : "2024-11-25T03:39Z",
                                        [
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(1),
                                                new OptionalPointDocumentFields(null, null)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(2),
                                                new OptionalPointDocumentFields("A03", null)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(3),
                                                new OptionalPointDocumentFields(null, 123.456m)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(4),
                                                new OptionalPointDocumentFields("A03", 654.321m)),
                                        ])),
                                new OptionalSeriesFields(
                                    isTransOne ? transactionId1 : transactionId2,
                                    "2022-12-17T09:30:47Z",
                                    null,
                                    null,
                                    "8716867000030")),
                        ])));
        }
    }

    [Theory(Skip = "Service bus is not used for metered data")]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task AndGiven_MessageIsEmpty_When_ActorPeeksAllMessages_Then_ReceivesNoMessages(
        DocumentFormat documentFormat)
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.GridAccessProvider);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        await GivenReceivedMeteredDataForMeteringPoint(
            documentFormat: documentFormat,
            senderActorNumber: currentActor.ActorNumber,
            []);

        await WhenMeteredDataForMeteringPointProcessIsInitialized(senderSpy.LatestMessage!);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            ActorNumber.Create("8100000000115"),
            ActorRole.EnergySupplier,
            documentFormat);

        // Assert
        peekResults.Should().BeEmpty();
    }
}
