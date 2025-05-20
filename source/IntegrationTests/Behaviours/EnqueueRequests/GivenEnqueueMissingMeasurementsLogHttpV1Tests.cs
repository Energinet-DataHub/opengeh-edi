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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_045;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM018;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.MissingMeasurementsLogCalculation.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.EnqueueRequests;

public class GivenEnqueueMissingMeasurementsLogHttpV1Tests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : BehavioursTestBase(
        integrationTestFixture,
        testOutputHelper)
{
    protected BundlingOptions BundlingOptions => GetService<IOptions<BundlingOptions>>().Value;

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_ValidRequest_When_GridAccessProviderPeeksMessages_Then_ReceivesCorrectReminderOfMissingMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var gridAccessProvider = new Actor(ActorNumber.Create("1111111111111"), ActorRole.GridAccessProvider);
        var meteringPointIdWithMissingData = MeteringPointId.From("123456789012345678");
        var missingDates = new List<DateTimeOffset>
        {
            DateTimeOffset.Parse("2023-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2023-01-02T00:00:00Z"),
            DateTimeOffset.Parse("2023-01-03T00:00:00Z"),
        };

        // Enqueue the missing measurements
        var whenMessagesAreEnqueued = Instant.FromUtc(2024, 7, 1, 14, 57, 09);
        GivenNowIs(whenMessagesAreEnqueued);
        await EnqueueMissingMeasurement(gridAccessProvider, missingDates, meteringPointIdWithMissingData);

        // Trigger the bundling
        var whenBundleShouldBeClosed = whenMessagesAreEnqueued.Plus(
            Duration.FromSeconds(BundlingOptions.BundleMessagesOlderThanSeconds));
        GivenNowIs(whenBundleShouldBeClosed);
        await GivenBundleMessagesHasBeenTriggered();

        // When (act)
        var peekResults = await WhenActorPeeksAllMessages(
            gridAccessProvider.ActorNumber,
            gridAccessProvider.ActorRole,
            documentFormat);

        var peekResult = peekResults.Should().ContainSingle().Subject;

        using var assertionScope = new AssertionScope();
        var assertMissingMeasurementProvider = AssertMissingMeasurementDocumentProvider.AssertDocument(
            peekResult.Bundle,
            documentFormat);

        await assertMissingMeasurementProvider
            .HasMessageId(peekResult.MessageId)
            .HasBusinessReason(BusinessReason.ReminderOfMissingMeasurementLog)
            .HasSenderId(DataHubDetails.DataHubActorNumber)
            .HasSenderRole(ActorRole.MeteredDataAdministrator)
            .HasReceiverId(gridAccessProvider.ActorNumber)
            .HasReceiverRole(ActorRole.MeteredDataResponsible)
            .HasTimestamp(whenBundleShouldBeClosed)
            .DocumentIsValidAsync();

        // The order of how the series are bundles seems random..
        // TODO: Nothing in computers are random.
        // for (int i = 0; i < missingDates.Count; i++)
        // {
        //     assertMissingMeasurementProvider
        //         .HasMeteringPointNumber(i + 1, meteringPointIdWithMissingData)
        //         .HasMissingDate(i + 1, missingDates[i].ToInstant());
        // }
    }

    private async Task EnqueueMissingMeasurement(
        Actor gridAccessProvider,
        List<DateTimeOffset> missingDates,
        MeteringPointId meteringPointIdWithMissingData)
    {
        var enqueueBrs045RequestFromProcessManager = new EnqueueMissingMeasurementsLogHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            Data: missingDates.Select(date =>
                new EnqueueMissingMeasurementsLogHttpV1.DateWithMeteringPointId(
                IdempotencyKey: Guid.NewGuid(),
                GridAccessProvider: gridAccessProvider.ActorNumber.ToProcessManagerActorNumber(),
                GridArea: "001",
                Date: date,
                MeteringPointId: meteringPointIdWithMissingData.Value)).ToList());

        var handler = GetService<EnqueueHandler_Brs_045_MissingMeasurementsLog>();

        await handler.HandleAsync(enqueueBrs045RequestFromProcessManager, CancellationToken.None);
    }
}
