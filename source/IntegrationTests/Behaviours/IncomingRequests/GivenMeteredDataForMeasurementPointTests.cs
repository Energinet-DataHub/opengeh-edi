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
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenMeteredDataForMeasurementPointTests : MeteredDataForMeasurementPointBehaviourTestBase
{
    public GivenMeteredDataForMeasurementPointTests(
        IntegrationTestFixture integrationTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    [Fact]
    public async Task When_ActorPeeksAllMessages_Then_ReceivesOneDocumentWithCorrectContent()
    {
        // Arrange
        var senderSpy = CreateServiceBusSenderSpy();
        var currentActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.GridAccessProvider);

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(currentActor.ActorNumber, currentActor.ActorRole);

        await GivenReceivedMeteredDataForMeasurementPoint(
            documentFormat: DocumentFormat.Xml,
            senderActorNumber: currentActor.ActorNumber,
            [
                ("12356478912356478912356478912356478",
                    InstantPattern.General.Parse("2024-11-28T13:51:42Z").Value,
                    InstantPattern.General.Parse("2024-11-29T09:15:28Z").Value,
                    Resolution.Hourly),
            ]);

        await WhenMeteredDataForMeasurementPointProcessIsInitialized(senderSpy.LatestMessage!);

        // ThenAggregatedTimeSeriesRequestServiceBusMessageIsCorrect

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            currentActor.ActorNumber,
            currentActor.ActorRole,
            DocumentFormat.Xml);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle("there should be one message given the MDFMP incoming message")
                .Subject;
        }

        peekResult.Bundle.Should().NotBeNull("peek result should contain a document stream");
    }
}
