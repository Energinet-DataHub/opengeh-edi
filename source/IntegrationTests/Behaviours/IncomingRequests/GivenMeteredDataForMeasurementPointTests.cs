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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Readability")]
public sealed class GivenMeteredDataForMeasurementPointTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : MeteredDataForMeasurementPointBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> PeekFormats =>
    [
        DocumentFormat.Json,
        DocumentFormat.Xml,
    ];

    [Theory]
    [MemberData(nameof(PeekFormats))]
    public async Task When_ActorPeeksAllMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat peekFormat)
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.GridAccessProvider);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        var transactionIdPrefix = Guid.NewGuid().ToString("N");

        await GivenReceivedMeteredDataForMeasurementPoint(
            documentFormat: DocumentFormat.Xml,
            senderActorNumber: currentActor.ActorNumber,
            [
                ($"{transactionIdPrefix}-1",
                    InstantPattern.General.Parse("2024-11-28T13:51:42Z").Value,
                    InstantPattern.General.Parse("2024-11-29T09:15:28Z").Value,
                    Resolution.Hourly),
                ($"{transactionIdPrefix}-2",
                    InstantPattern.General.Parse("2024-11-24T18:51:58Z").Value,
                    InstantPattern.General.Parse("2024-11-25T03:39:45Z").Value,
                    Resolution.QuarterHourly),
            ]);

        await WhenMeteredDataForMeasurementPointProcessIsInitialized(senderSpy.LatestMessage!);

        // ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            ActorNumber.Create("8100000000115"),
            ActorRole.EnergySupplier,
            peekFormat);

        // Assert
        foreach (var peekResultDto in peekResults)
        {
            // This is not pretty, but it works for now
            var foo = new StreamReader(peekResultDto.Bundle);
            var content = await foo.ReadToEndAsync();
            var isTransOne = content.Contains($"{transactionIdPrefix}-1");
            peekResultDto.Bundle.Position = 0;

            await ThenNotifyValidatedMeasureDataDocumentIsCorrect(
                peekResultDto.Bundle,
                peekFormat,
                new NotifyValidatedMeasureDataDocumentAssertionInput(
                    new RequiredDocumentFields(
                        "E23",
                        "8100000000115",
                        "A10",
                        "5790001330552",
                        "A10",
                        "DGL",
                        "DDQ",
                        "2024-07-01T14:57:09Z"),
                    isTransOne
                        ? new RequiredSeriesFields(
                            TransactionId.From(string.Join(string.Empty, $"{transactionIdPrefix}-1".Reverse())),
                            "579999993331812345",
                            "A10",
                            "E17",
                            "KWH",
                            "PT1H",
                            "2024-11-28T13:51Z",
                            "2024-11-29T09:15Z",
                            [])
                        : new RequiredSeriesFields(
                            TransactionId.From(string.Join(string.Empty, $"{transactionIdPrefix}-2".Reverse())),
                            "579999993331812345",
                            "A10",
                            "E17",
                            "KWH",
                            "PT15M",
                            "2024-11-24T18:51Z",
                            "2024-11-25T03:39Z",
                            [])));
        }
    }
}
