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
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.Bundling;

public class WhenEnqueuingMeasureDataWithBundlingTests : OutgoingMessagesTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ClockStub _clockStub;

    public WhenEnqueuingMeasureDataWithBundlingTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _clockStub = (ClockStub)GetService<IClock>();
    }

    public static TheoryData<Func<Actor, OutgoingMessageDto>> MessageBuildersForBundledMessageTypes()
    {
        // TODO: Add rejected bundling as well.
        return new([
                receiver => new AcceptedForwardMeteredDataMessageDtoBuilder()
                    .WithReceiver(receiver)
                    .Build(),
            ]);
    }

    [Theory]
    [MemberData(nameof(MessageBuildersForBundledMessageTypes))]
    public async Task Given_EnqueuedTwoMessageForSameBundle_When_BundleMessages_Then_BothMessageAreInTheSameBundle_AndThen_BundleHasCorrectCount(
        Func<Actor, OutgoingMessageDto> messageBuilder)
    {
        // Given existing bundle
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // - Create two message for same bundle
        var message1 = messageBuilder(receiver);
        var message2 = messageBuilder(receiver);

        // - Enqueue messages "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);
        await EnqueueAndCommitMessage(message1, useNewScope: false);
        await EnqueueAndCommitMessage(message2, useNewScope: false);

        // When creating bundles

        // - Move clock to when bundles should be created
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Create bundles
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);

        // Then message is added to existing bundle & bundle has correct count
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .ToListAsync();

        // - One bundle was created
        Assert.Single(bundles);

        // - Both outgoing messages are in the same bundle (have the correct bundle id)
        var bundle = bundles.Single();
        Assert.Multiple(
            () => Assert.Equal(2, outgoingMessages.Count),
            () => Assert.All(outgoingMessages, om => Assert.Equal(bundle.Id, om.AssignedBundleId)));
    }

    [Theory]
    [MemberData(nameof(MessageBuildersForBundledMessageTypes))]
    public async Task Given_EnqueuedTwoMessageForDifferentReceivers_When_BundlingMessages_Then_TheABundleIsCreatedForEachMessage(
        Func<Actor, OutgoingMessageDto> messageBuilder)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var receiver1 = new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var receiver2 = new Actor(ActorNumber.Create("2222222222222"), ActorRole.EnergySupplier);

        // - Create two message for same bundle
        var message1 = messageBuilder(receiver1);
        var message2 = messageBuilder(receiver2);

        // - Enqueue messages "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);
        await EnqueueAndCommitMessage(message1, useNewScope: false);
        await EnqueueAndCommitMessage(message2, useNewScope: false);

        // When creating bundles
        // - Move clock to when bundles should be created
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Create bundles
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);

        // Then a bundle is created for each message
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .ToListAsync();

        // - Two bundles are created, each having 1 message.
        Assert.Multiple(
            () => Assert.Equal(2, bundles.Count),
            () => Assert.NotEqual(outgoingMessages[0].AssignedBundleId, outgoingMessages[1].AssignedBundleId),
            () => Assert.All(bundles, b => Assert.Single(outgoingMessages, om => om.AssignedBundleId == b.Id)));
    }

    [Theory]
    [MemberData(nameof(MessageBuildersForBundledMessageTypes))]
    public async Task Given_EnqueuedMessagesForTwoBundles_AndGiven_PartialBundleDoesNotHaveMessageOldEnoughToBeBundled_When_BundleMessages_Then_OnlyOneBundleIsCreatedWithCorrectCount(
        Func<Actor, OutgoingMessageDto> messageBuilder)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // - Create messages for two bundles
        var messagesForBundle1 = Enumerable.Range(0, bundlingOptions.MaxBundleSize)
            .Select(_ => messageBuilder(receiver))
            .ToList();
        var messageForPartialBundle = messageBuilder(receiver);

        // - Enqueue messages for bundle 1 "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);
        await EnqueueAndCommitMessages(messagesForBundle1);

        var whenBundle1ShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));

        // - Enqueue message for partial bundle later, so it shouldn't be bundled yet
        _clockStub.SetCurrentInstant(whenBundle1ShouldBeCreated.Minus(Duration.FromMilliseconds(1)));
        await EnqueueAndCommitMessage(messageForPartialBundle, useNewScope: false);

        // When creating bundles

        // - Move clock to when bundle 1 should be created
        _clockStub.SetCurrentInstant(whenBundle1ShouldBeCreated);

        // - Create bundles
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);

        // Then message is added to existing bundle & bundle has correct count
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .ToListAsync();

        // - Only one bundle was created
        // - All outgoing messages are enqueued
        // - Only one message is not assigned a bundle
        // - The bundle has a count of MaxBundleSize
        Assert.Multiple(
            () => Assert.Single(bundles),
            () => Assert.Equal(bundlingOptions.MaxBundleSize + 1, outgoingMessages.Count),
            () => Assert.Single(outgoingMessages, om => om.AssignedBundleId == null),
            () => Assert.Collection(
                collection: bundles,
                elementInspectors: b => Assert.Equal(
                    expected: bundlingOptions.MaxBundleSize,
                    actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id))));
    }

    [Theory]
    [MemberData(nameof(MessageBuildersForBundledMessageTypes))]
    public async Task Given_EnqueuedMessagesForTwoBundles_AndGiven_PartialBundleHasMessageOldEnoughToBeBundled_When_BundleMessages_Then_TwoBundlesAreCreatedWithCorrectCount(
        Func<Actor, OutgoingMessageDto> messageBuilder)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // - Create messages for two bundles
        var messagesForBundle1 = Enumerable.Range(0, bundlingOptions.MaxBundleSize)
            .Select(_ => messageBuilder(receiver))
            .ToList();
        var message1ForPartialBundle = messageBuilder(receiver);
        var message2ForPartialBundle = messageBuilder(receiver);

        // - Enqueue messages for bundle 1 "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);
        await EnqueueAndCommitMessages(messagesForBundle1);

        // - Enqueue message for partial bundle "now", so it is old enough to be bundled
        await EnqueueAndCommitMessage(message1ForPartialBundle, useNewScope: false);

        var whenBundle1ShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));

        // - Enqueue message for partial bundle later, so it shouldn't be bundled yet. This should still be bundled
        // because message1ForPartialBundle is old enough to be bundled.
        _clockStub.SetCurrentInstant(whenBundle1ShouldBeCreated.Minus(Duration.FromMilliseconds(1)));
        await EnqueueAndCommitMessage(message2ForPartialBundle, useNewScope: false);

        // When creating bundles

        // - Move clock to when bundle 1 should be created
        _clockStub.SetCurrentInstant(whenBundle1ShouldBeCreated);

        // - Create bundles
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);

        // Then message is added to existing bundle & bundle has correct count
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .ToListAsync();

        // - Two bundles was created
        // - All outgoing messages are enqueued
        // - All outgoing messages are bundled
        // - MaxBundleSize messages are assigned bundle 1
        // - 2 messages are assigned bundle 2
        Assert.Multiple(
            () => Assert.Equal(2, bundles.Count),
            () => Assert.Equal(bundlingOptions.MaxBundleSize + 2, outgoingMessages.Count),
            () => Assert.All(outgoingMessages, om => Assert.NotNull(om.AssignedBundleId)),
            () => Assert.Collection(
                collection: bundles.OrderByDescending(b => outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                elementInspectors:
                [
                    // First bundle has MaxBundleSize messages
                    b => Assert.Equal(
                        expected: bundlingOptions.MaxBundleSize,
                        actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                    // Second bundle has 2 messages
                    b => Assert.Equal(
                        expected: 2,
                        actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                ]));
    }

    [Fact]
    public async Task Given_MessagesEnqueuedForTwoDifferentReceivers_When_BundleMessages_Then_AllMessagesAreInCorrectBundles()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given messages enqueued for two different receivers
        var receiver1 = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var receiver2 = Receiver.Create(ActorNumber.Create("2222222222222"), ActorRole.EnergySupplier);

        // - Create actor message queue for receivers
        using (var setupScope = ServiceProvider.CreateScope())
        {
            var amqContext = setupScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(receiver1));
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(receiver2));
            await amqContext.SaveChangesAsync();
        }

        // - Create messages for receivers
        var eventId = EventId.From(Guid.NewGuid());
        var startTime = Instant.FromUtc(2024, 03, 21, 23, 00, 00);
        var bundleSize = bundlingOptions.MaxBundleSize; // Max bundle size = 2000
        const int receiver1MessageCount = 7234; // 4 bundles for receiver 1
        var receiver1BundleCount = (int)Math.Ceiling((double)receiver1MessageCount / bundleSize);
        const int receiver2MessageCount = 1111; // 1 bundle for receiver 2
        var receiver2BundleCount = (int)Math.Ceiling((double)receiver2MessageCount / bundleSize);
        const int totalMessageCount = receiver1MessageCount + receiver2MessageCount;
        var totalBundleCount = receiver1BundleCount + receiver2BundleCount;
        var messagesToEnqueue = Enumerable.Range(0, totalMessageCount)
            .Select(
                i =>
                {
                    var receiver = i < receiver1MessageCount ? receiver1 : receiver2;

                    var resolutionDuration = Duration.FromMinutes(15);
                    var time = startTime.Plus(i * resolutionDuration); // Start every message 15 minutes later
                    return new AcceptedForwardMeteredDataMessageDto(
                        eventId: eventId,
                        externalId: new ExternalId(Guid.NewGuid()),
                        receiver: receiver.ToActor(),
                        businessReason: BusinessReason.PeriodicMetering,
                        relatedToMessageId: MessageId.New(),
                        series: new ForwardMeteredDataMessageSeriesDto(
                            TransactionId: TransactionId.New(),
                            MarketEvaluationPointNumber: "1234567890123",
                            MarketEvaluationPointType: MeteringPointType.Consumption,
                            OriginalTransactionIdReferenceId: TransactionId.New(),
                            Product: "test-product",
                            QuantityMeasureUnit: MeasurementUnit.KilowattHour,
                            RegistrationDateTime: time,
                            Resolution: Resolution.QuarterHourly,
                            StartedDateTime: time,
                            EndedDateTime: time.Plus(resolutionDuration),
                            EnergyObservations:
                            [
                                new EnergyObservationDto(1, 1, Quality.Calculated),
                            ]));
                })
            .Cast<OutgoingMessageDto>()
            .ToList();

        // - Set messages created at to "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);

        // - Enqueue messages for receivers
        var enqueueStopwatch = Stopwatch.StartNew();
        var enqueuedMessagesCount = await EnqueueAndCommitMessages(
            messagesToEnqueue: messagesToEnqueue);
        enqueueStopwatch.Stop();

        _testOutputHelper.WriteLine("Finished enqueueing {0} messages. Elapsed time: {1:D2}m{2:D2}s", enqueuedMessagesCount, enqueueStopwatch.Elapsed.Minutes, enqueueStopwatch.Elapsed.Seconds);

        // When bundling the messages
        // - Move clock to when bundles should be created
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Bundle messages
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();

        var bundleStopwatch = Stopwatch.StartNew();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);
        bundleStopwatch.Stop();

        // Then all messages are in correct bundles
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var nullBundleKey = BundleId.Create(Guid.Empty);
        var outgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .GroupBy(om => om.AssignedBundleId)
            .ToDictionaryAsync(
                keySelector: om => om.Key ?? nullBundleKey,
                elementSelector: om => om.ToList());

        var bundles = await dbContext.Bundles
            .Where(b => b.MessageCategory == MessageCategory.MeasureData)
            .ToListAsync();

        _testOutputHelper.WriteLine(
            "Created {0} bundles (for {1} messages). Elapsed time: {2:D2}m{3:D2}s{4:D2}ms",
            bundles.Count,
            enqueuedMessagesCount,
            bundleStopwatch.Elapsed.Minutes,
            bundleStopwatch.Elapsed.Seconds,
            bundleStopwatch.Elapsed.Milliseconds);

        Assert.Multiple(
            () => Assert.Equal(totalMessageCount, outgoingMessages.SelectMany(om => om.Value).Count()), // All messages are enqueued
            () => Assert.DoesNotContain(outgoingMessages, om => om.Key == nullBundleKey), // All messages are assigned a bundle
            () => Assert.Equal(totalBundleCount, bundles.Count), // The created bundle count is as expected
            () => Assert.Collection(
                bundles.OrderByDescending(b => outgoingMessages[b.Id].Count),
                // The outgoing messages are in the correct bundles
                b =>
                {
                    // 2000 messages for receiver 1
                    Assert.Multiple(
                        () => Assert.True(
                                outgoingMessages[b.Id].Count == bundleSize,
                                $"1st bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}"),
                        () => Assert.All(outgoingMessages[b.Id], om => Assert.Equal(receiver1, om.Receiver)));
                },
                b =>
                {
                    // 2000 messages for receiver 1
                    Assert.Multiple(
                        () => Assert.True(
                            outgoingMessages[b.Id].Count == bundleSize,
                            $"1st bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}"),
                        () => Assert.All(outgoingMessages[b.Id], om => Assert.Equal(receiver1, om.Receiver)));
                },
                b =>
                {
                    // 2000 messages for receiver 1
                    Assert.Multiple(
                        () => Assert.True(
                            outgoingMessages[b.Id].Count == bundleSize,
                            $"1st bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}"),
                        () => Assert.All(outgoingMessages[b.Id], om => Assert.Equal(receiver1, om.Receiver)));
                },
                b =>
                {
                    // 1234 messages for receiver 1
                    Assert.Multiple(
                        () => Assert.True(
                            outgoingMessages[b.Id].Count == receiver1MessageCount % bundleSize,
                            $"4th bundle count should be {receiver1MessageCount % bundleSize}, but was {outgoingMessages[b.Id].Count}"),
                        () => Assert.All(outgoingMessages[b.Id], om => Assert.Equal(receiver1, om.Receiver)));
                },
                b =>
                {
                    // 1111 messages for receiver 2
                    Assert.True(
                        outgoingMessages[b.Id].Count == receiver2MessageCount % bundleSize,
                        $"5th bundle count should be {receiver2MessageCount % bundleSize}, but was {outgoingMessages[b.Id].Count}");
                    Assert.All(outgoingMessages[b.Id], om => Assert.Equal(receiver2, om.Receiver));
                }));
    }

    private async Task<int> EnqueueAndCommitMessages(List<OutgoingMessageDto> messagesToEnqueue)
    {
        var enqueueTasks = messagesToEnqueue
            .Select(m => EnqueueAndCommitMessage(m, useNewScope: true))
            .ToList();
        await Task.WhenAll(enqueueTasks);

        return enqueueTasks.Count;
    }

    private async Task<Guid> EnqueueAndCommitMessage(OutgoingMessageDto message, bool useNewScope)
    {
        IOutgoingMessagesClient outgoingMessagesClient;
        ActorMessageQueueContext dbContext;
        IServiceScope? scope = null;
        if (useNewScope)
        {
            scope = ServiceProvider.CreateScope();
            outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
            dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        }
        else
        {
            outgoingMessagesClient = ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
            dbContext = ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        }

        var messageId = message switch
        {
            AcceptedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAsync(m, CancellationToken.None),
            RejectedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAndCommitAsync(m, CancellationToken.None),
            _ => throw new NotImplementedException($"Enqueueing outgoing message of type {message.GetType()} is not implemented."),
        };

        await dbContext.SaveChangesAsync();

        scope?.Dispose();

        return messageId;
    }
}
