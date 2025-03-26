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

using System.Diagnostics;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.LoadTest;

public class BundleForwardMeteredDataTests : IClassFixture<LoadTestFixture>
{
    private const string AverageBundleTimeMetric = "AverageBundleTime";
    private const string OriginalActorMessageIdPrefix = "bundle_perf_test_"; // Used to clean up previous test messages before running new test

    private readonly LoadTestFixture _fixture;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;
    private readonly ProcessManagerDriver _processManagerDriver;

    public BundleForwardMeteredDataTests(LoadTestFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _testOutputHelper = testOutputHelper;
        _ediDatabaseDriver = new EdiDatabaseDriver(_fixture.DatabaseConnectionString);
        _processManagerDriver = new ProcessManagerDriver(fixture.EdiTopicClient);
    }

    [Fact]
    public async Task Given_MultipleForwardMeteredDataMessages_When_EnqueuedConcurrently_Then_MessagesAreEnqueuedInAllowedTime()
    {
        await CleanUp();

        const int messagesToEnqueueCount = 10000;

        var eventId = Guid.NewGuid(); // Used to find the enqueued outgoing messages in the database

        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var start = Instant.FromUtc(2024, 12, 31, 23, 00, 00);
        var end = start.Plus(Duration.FromDays(7));

        // Add all enqueue messages to EDI service bus topic

        // Start stopwatch here, since as soon as the first service bus message is sent, EDI starts enqueueing messages.
        var stopwatch = Stopwatch.StartNew();

        await _processManagerDriver.PublishEnqueueBrs021AcceptedForwardMeteredDataRequestsAsync(
            Enumerable.Range(0, messagesToEnqueueCount)
                .Select(i => (
                    Actor: receiver,
                    Start: start,
                    End: end,
                    OriginalActorMessageId: OriginalActorMessageIdPrefix + i,
                    EventId: eventId))
                .ToList());

        // Wait for all messages to be enqueued (with a timeout)
        var timeout = TimeSpan.FromMinutes(15);
        var timeoutCancellationToken = new CancellationTokenSource(timeout).Token;

        List<EdiDatabaseDriver.OutgoingMessageDto> enqueuedMessages = [];
        while (!timeoutCancellationToken.IsCancellationRequested)
        {
            var previousCount = enqueuedMessages.Count;
            // Get outgoing messages & bundles from database
            enqueuedMessages = await _ediDatabaseDriver.GetNotifyValidatedMeasureDataMessagesFromLoadTestAsync(eventId);

            _testOutputHelper.WriteLine(
                "Enqueued messages count: {0} of {1} (elapsed time: {2:g})",
                enqueuedMessages.Count,
                messagesToEnqueueCount,
                stopwatch.Elapsed);

            var enqueuedMessagesCount = enqueuedMessages.Count;
            var finishedEnqueueing = enqueuedMessagesCount >= messagesToEnqueueCount;
            var stoppedEnqueuing = enqueuedMessagesCount == previousCount;
            if (finishedEnqueueing || stoppedEnqueuing || timeoutCancellationToken.IsCancellationRequested)
            {
                _testOutputHelper.WriteLine(
                    "Stopped enqueueing outgoing messages (enqueued {0} in {1:g} minutes, finishedEnqueueing={2}, stoppedEnqueuing={3}, cancellationTimeout={4})",
                    enqueuedMessages.Count,
                    stopwatch.Elapsed,
                    finishedEnqueueing,
                    stoppedEnqueuing,
                    timeoutCancellationToken.IsCancellationRequested);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        Assert.NotEmpty(enqueuedMessages);

        var startedAt = enqueuedMessages.First().CreatedAt;
        var finishedAt = enqueuedMessages.First().CreatedAt;
        var bundleIds = new HashSet<Guid>();
        foreach (var outgoingMessage in enqueuedMessages)
        {
            if (outgoingMessage.CreatedAt < startedAt)
                startedAt = outgoingMessage.CreatedAt;

            if (outgoingMessage.CreatedAt > finishedAt)
                finishedAt = outgoingMessage.CreatedAt;

            // HashSet does nothing if the bundle id is already added, so we do not need to check for duplicates.
            bundleIds.Add(outgoingMessage.AssignedBundleId);
        }

        var bundles = await _ediDatabaseDriver.GetNotifyValidatedMeasureDataBundlesAsync(bundleIds);

        var enqueueTime = finishedAt - startedAt;

        _testOutputHelper.WriteLine(
            "Enqueued {0} outgoing messages (in {1} bundles), took {2:F2} minutes",
            enqueuedMessages.Count,
            bundles.Count,
            enqueueTime.TotalMinutes);

        _fixture.TelemetryClient.GetMetric(AverageBundleTimeMetric).TrackValue(enqueueTime.TotalMilliseconds / (double)enqueuedMessages.Count);

        Assert.Multiple(
            () => Assert.Equal(messagesToEnqueueCount, enqueuedMessages.Count),
            () => Assert.True(bundles.Count is 5 or 6, $"Messages should be enqueued in 5 or 6 bundles, but was {bundles.Count}"),
            () => Assert.True(enqueueTime < TimeSpan.FromMinutes(5), $"Enqueue time should be less than 5 minutes, but was {enqueueTime:g}"));

        await CleanUp();
    }

    private async Task CleanUp()
    {
        await _ediDatabaseDriver.MarkBundlesFromLoadTestAsDequeuedAMonthAgoAsync(OriginalActorMessageIdPrefix, CancellationToken.None);
    }
}
