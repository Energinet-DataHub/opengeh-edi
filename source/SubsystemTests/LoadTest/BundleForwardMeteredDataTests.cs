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
    private const string OriginalActorMessageIdPrefix = "bundle_perf_test_"; // Used to clean up previous test messages before running new test

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly EdiDatabaseDriver _ediDatabaseDriver;
    private readonly ProcessManagerDriver _processManagerDriver;

    public BundleForwardMeteredDataTests(LoadTestFixture fixture, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _ediDatabaseDriver = new EdiDatabaseDriver(fixture.DatabaseConnectionString);
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
            var previousBundledCount = enqueuedMessages.Count(b => b.AssignedBundleId != null);
            // Get outgoing messages & bundles from database
            enqueuedMessages = await _ediDatabaseDriver.GetNotifyValidatedMeasureDataMessagesFromLoadTestAsync(eventId);

            var enqueuedMessagesCount = enqueuedMessages.Count;
            var bundledMessagesCount = enqueuedMessages.Count(m => m.AssignedBundleId != null);

            _testOutputHelper.WriteLine(
                "Bundled message count: {0}, enqueued messages count: {1} of {2} (elapsed time: {3:g})",
                bundledMessagesCount,
                enqueuedMessages.Count,
                messagesToEnqueueCount,
                stopwatch.Elapsed);

            var finishedBundling = bundledMessagesCount >= messagesToEnqueueCount;
            var stoppedBundling = bundledMessagesCount > 00 && bundledMessagesCount == previousBundledCount;
            if (finishedBundling || stoppedBundling || timeoutCancellationToken.IsCancellationRequested)
            {
                _testOutputHelper.WriteLine(
                    "Stopped enqueueing/bundling outgoing messages (enqueued {0}, bundled {5} in {1:g} minutes, finishedBundling={2}, stoppedEnqueuing={3}, cancellationTimeout={4})",
                    enqueuedMessagesCount,
                    stopwatch.Elapsed,
                    finishedBundling,
                    stoppedBundling,
                    timeoutCancellationToken.IsCancellationRequested,
                    bundledMessagesCount);
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        Assert.NotEmpty(enqueuedMessages);
        Assert.All(enqueuedMessages, m => Assert.NotNull(m.AssignedBundleId));

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
            bundleIds.Add(outgoingMessage.AssignedBundleId!.Value);
        }

        var bundles = await _ediDatabaseDriver.GetNotifyValidatedMeasureDataBundlesAsync(bundleIds);

        var enqueueTime = finishedAt - startedAt;

        _testOutputHelper.WriteLine(
            "Enqueued {0} outgoing messages (in {1} bundles), took {2:F2} minutes",
            enqueuedMessages.Count,
            bundles.Count,
            enqueueTime.TotalMinutes);

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
