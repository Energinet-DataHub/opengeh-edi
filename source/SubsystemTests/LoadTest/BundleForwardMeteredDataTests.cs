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

        const string originalActorMessageIdPrefix = "bundle_perf_test_"; // Used to clean up previous test messages before running new test
        var eventId = Guid.NewGuid(); // Used to find the enqueued outgoing messages in the databse

        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var start = Instant.FromUtc(2024, 12, 31, 23, 00, 00);
        var end = start.Plus(Duration.FromDays(7));

        // Add enqueue messages to EDI service bus topic
        var tasks = Enumerable.Range(0, messagesToEnqueueCount)
            .Select(
                i =>
                {
                    var task = _processManagerDriver.PublishEnqueueBrs021AcceptedForwardMeteredDataRequestAsync(
                        actor: receiver,
                        start: start,
                        end: end,
                        originalActorMessageId: originalActorMessageIdPrefix + i,
                        eventId: eventId);
                    return task;
                })
            .ToList();

        // Wait for all enqueue messages to be added to the service bus
        await Task.WhenAll(tasks);

        // Wait for all messages to be enqueued
        var timeout = TimeSpan.FromMinutes(5);
        var timeoutCancellationToken = new CancellationTokenSource(timeout).Token;

        var stopwatch = Stopwatch.StartNew();
        List<EdiDatabaseDriver.OutgoingMessageDto> enqueuedMessages = [];
        while (!timeoutCancellationToken.IsCancellationRequested)
        {
            // Get outgoing messages & bundles from database
            enqueuedMessages = await _ediDatabaseDriver.GetNotifyValidatedMeasureDataMessagesFromLoadTestAsync(eventId);

            _testOutputHelper.WriteLine(
                "Enqueued messages count: {0} of {1} (elapsed time: {2:g})",
                enqueuedMessages.Count,
                messagesToEnqueueCount,
                stopwatch.Elapsed);

            if (enqueuedMessages.Count >= messagesToEnqueueCount || timeoutCancellationToken.IsCancellationRequested)
                break;

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
    }

    private async Task CleanUp()
    {
        await _ediDatabaseDriver.MarkBundlesFromLoadTestAsDequeuedAMonthAgoAsync(CancellationToken.None);
    }
}
