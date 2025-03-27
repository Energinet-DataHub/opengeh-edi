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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
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
    public async Task Given_EnqueuedTwoMessageForSameBundle_When_BundlingMessages_Then_BothMessageAreInTheSameBundle_AndThen_BundleHasCorrectCount(
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
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromMinutes(bundlingOptions.BundleDurationInMinutes));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Create bundles
        var bundleMessages = ServiceProvider.GetRequiredService<BundleMessages>();
        await bundleMessages.BundleMessagesAsync(CancellationToken.None);

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

    [Fact]
    public async Task Given_EnqueuedMultipleMessagesToDifferentReceivers_When_BundleMessages_Then_AllMessagesAreBundlesInCorrectBundles()
    {
        var receiver1 = new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var receiver2 = new Actor(ActorNumber.Create("2222222222222"), ActorRole.EnergySupplier);

        // Create actor message queue for actor
        using (var setupScope = ServiceProvider.CreateScope())
        {
            var amqContext = setupScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(Receiver.Create(receiver1)));
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(Receiver.Create(receiver2)));
            await amqContext.SaveChangesAsync();
        }

        var eventId = EventId.From(Guid.NewGuid());
        var startTime = Instant.FromUtc(2024, 03, 21, 23, 00, 00);
        const int bundleSize = 2000;
        const int receiver1MessageCount = 7234; // 4 bundles for receiver 1
        var receiver1BundleCount = (int)Math.Ceiling((double)receiver1MessageCount / bundleSize);
        const int receiver2MessageCount = 1689; // 1 bundle for receiver 2
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
                        receiver: receiver,
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
            .ToList();

        // When enqueueing the messages concurrently
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);

        var enqueueStopwatch = Stopwatch.StartNew();
        var enqueuedMessagesCount = await EnqueueMessages(
            messagesToEnqueue: messagesToEnqueue);
        enqueueStopwatch.Stop();

        _testOutputHelper.WriteLine("Finished enqueueing {0} messages. Elapsed time: {1:D2}m{2:D2}s", enqueuedMessagesCount, enqueueStopwatch.Elapsed.Minutes, enqueueStopwatch.Elapsed.Seconds);

        // When bundling the messages
        // - Move clock to when bundles should be created
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromMinutes(bundlingOptions.BundleDurationInMinutes));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Bundle messages
        var bundleMessages = ServiceProvider.GetRequiredService<BundleMessages>();

        var bundleStopwatch = Stopwatch.StartNew();
        await bundleMessages.BundleMessagesAsync(CancellationToken.None);
        bundleStopwatch.Stop();

        // Then all messages are enqueued & 3 bundles created
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
            () => Assert.Equal(totalMessageCount, outgoingMessages.SelectMany(om => om.Value).Count()),
            () => Assert.DoesNotContain(outgoingMessages, om => om.Key == nullBundleKey),
            () => Assert.Equal(totalBundleCount, bundles.Count),
            () => Assert.Collection(
                bundles.OrderByDescending(b => outgoingMessages[b.Id].Count),
                b =>
                {
                    Assert.True(outgoingMessages[b.Id].Count == bundleSize, $"1st bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}");
                },
                b =>
                {
                    Assert.True(outgoingMessages[b.Id].Count == bundleSize, $"2nd bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}");
                },
                b =>
                {
                    Assert.True(outgoingMessages[b.Id].Count == bundleSize, $"3rd bundle count should be {bundleSize}, but was {outgoingMessages[b.Id].Count}");
                },
                b =>
                {
                    Assert.True(outgoingMessages[b.Id].Count == receiver2MessageCount % bundleSize, $"4th bundle count should be {receiver2MessageCount % bundleSize}, but was {outgoingMessages[b.Id].Count}");
                },
                b =>
                {
                    Assert.True(outgoingMessages[b.Id].Count == receiver1MessageCount % bundleSize, $"5th bundle count should be {receiver1MessageCount % bundleSize}, but was {outgoingMessages[b.Id].Count}");
                }));
    }

    private async Task<int> EnqueueMessages(List<AcceptedForwardMeteredDataMessageDto> messagesToEnqueue)
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
