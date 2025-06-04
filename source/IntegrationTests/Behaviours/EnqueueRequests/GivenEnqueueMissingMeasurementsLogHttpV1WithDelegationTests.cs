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
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.Shared;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.EnqueueRequests;

public class GivenEnqueueMissingMeasurementsLogHttpV1WithDelegationTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : BehavioursTestBase(
        integrationTestFixture,
        testOutputHelper)
{
    protected BundlingOptions BundlingOptions => GetService<IOptions<BundlingOptions>>().Value;

    [Theory]
    [MemberData(nameof(DocumentFormats.AllDocumentFormats), MemberType = typeof(DocumentFormats))]
    public async Task AndGiven_ThreeMissingDates_When_GridAccessProviderPeeksMessages_Then_ReceivesCorrectReminderOfMissingMeasureDataDocuments(DocumentFormat documentFormat)
    {
        // Given (arrange)
        var gridAccessProviderWhichDelegates = new Actor(
            ActorNumber.Create("10X123456723432S"), // This is an Eic code
            ActorRole.GridAccessProvider);
        var delegatedActor = new Actor(
            ActorNumber.Create("10X------------J"), // This is an Eic code
            ActorRole.Delegated);
        var gridAreaCode = "001";
        var meteringPointIdWithMissingData = new List<(MeteringPointId MeteringPointId, Instant Date)>
        {
            (MeteringPointId.From("123456789012345671"), DateTimeOffset.Parse("2023-01-01T00:00:00Z").ToInstant()),
            (MeteringPointId.From("123456789012345672"), DateTimeOffset.Parse("2023-01-02T00:00:00Z").ToInstant()),
            (MeteringPointId.From("123456789012345673"), DateTimeOffset.Parse("2023-01-03T00:00:00Z").ToInstant()),
        };

        var delegationStartsAt = Instant.FromUtc(2024, 6, 1, 14, 57, 09);

        await GivenDelegation(
            delegatedBy: gridAccessProviderWhichDelegates,
            delegatedTo: delegatedActor,
            gridAreaCode,
            processType: ProcessType.MissingMeasurementLog,
            startsAt: delegationStartsAt);

        // Enqueue the missing measurements messages
        var whenMessagesAreEnqueued = delegationStartsAt.PlusDays(2);
        GivenNowIs(whenMessagesAreEnqueued);
        await EnqueueMissingMeasurements(gridAccessProviderWhichDelegates, meteringPointIdWithMissingData, gridAreaCode);

        // Trigger the bundling
        var whenBundleShouldBeClosed = whenMessagesAreEnqueued.Plus(
            Duration.FromSeconds(BundlingOptions.BundleMessagesOlderThanSeconds));
        GivenNowIs(whenBundleShouldBeClosed);
        await GivenBundleMessagesHasBeenTriggered();

        // When (act)
        var peekResultsForDelegatingActor = await WhenActorPeeksAllMessages(
            gridAccessProviderWhichDelegates.ActorNumber,
            gridAccessProviderWhichDelegates.ActorRole,
            documentFormat);

        var peekResultsForDelegatedActor = await WhenActorPeeksAllMessages(
            delegatedActor.ActorNumber,
            delegatedActor.ActorRole,
            documentFormat);

        // Assert
        peekResultsForDelegatingActor.Should().BeEmpty();
        var peekResult = peekResultsForDelegatedActor.Should().ContainSingle().Subject;

        using var assertionScope = new AssertionScope();
        var assertMissingMeasurementProvider = AssertMissingMeasurementDocumentProvider.AssertDocument(
            peekResult.Bundle,
            documentFormat);

        await assertMissingMeasurementProvider
            .HasMessageId(peekResult.MessageId)
            .HasBusinessReason(BusinessReason.ReminderOfMissingMeasurementLog)
            .HasSenderId(DataHubDetails.DataHubActorNumber)
            .HasSenderRole(ActorRole.MeteredDataAdministrator)
            .HasReceiverId(delegatedActor.ActorNumber)
            .HasReceiverRole(ActorRole.MeteredDataResponsible)
            .HasTimestamp(whenBundleShouldBeClosed)
            .HasMissingData(meteringPointIdWithMissingData)
            .DocumentIsValidAsync();
    }

    private async Task EnqueueMissingMeasurements(
        Actor gridAccessProvider,
        List<(MeteringPointId MeteringPointId, Instant Date)> meteringPointIdWithMissingData,
        string gridAreaCode = "001")
    {
        var enqueueBrs045RequestFromProcessManager = new EnqueueMissingMeasurementsLogHttpV1(
            OrchestrationInstanceId: Guid.NewGuid(),
            Data: meteringPointIdWithMissingData.Select(date =>
                new EnqueueMissingMeasurementsLogHttpV1.DateWithMeteringPointId(
                GridAccessProvider: gridAccessProvider.ActorNumber.ToProcessManagerActorNumber(),
                GridArea: gridAreaCode,
                Date: date.Date.ToDateTimeOffset(),
                MeteringPointId: date.MeteringPointId.Value)).ToList());

        var handler = GetService<EnqueueHandler_Brs_045_MissingMeasurementsLog>();

        await handler.HandleAsync(enqueueBrs045RequestFromProcessManager, CancellationToken.None);
    }
}
