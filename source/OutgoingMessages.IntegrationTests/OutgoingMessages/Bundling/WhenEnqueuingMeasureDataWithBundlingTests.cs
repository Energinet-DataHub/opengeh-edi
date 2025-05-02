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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
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

    public static TheoryData<DocumentType> DocumentTypesForBundledMessageTypes()
    {
        return new([
                DocumentType.NotifyValidatedMeasureData,
                DocumentType.Acknowledgement,
            ]);
    }

    public static TheoryData<ActorRole> ReceiverActorRolesForNotifyValidatedMeasureData()
    {
        return new([
            ActorRole.EnergySupplier,
            ActorRole.GridAccessProvider,
            ActorRole.DanishEnergyAgency,
            ActorRole.SystemOperator,
        ]);
    }

    public static TheoryData<ActorRole> ReceiverActorRolesForAcknowledgement()
    {
        return new([
            ActorRole.MeteredDataResponsible, // RSM-009 messages are sent to the sender role of the incoming RSM-012, which is MDR.
        ]);
    }

    [Theory]
    [MemberData(nameof(DocumentTypesForBundledMessageTypes))]
    public async Task Given_EnqueuedTwoMessageForSameBundle_When_BundleMessages_Then_BothMessageAreInTheSameBundle_AndThen_BundleHasCorrectCount(
        DocumentType documentType)
    {
        // Given existing bundle
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var relatedToMessageId1 = MessageId.New();
        var relatedToMessageId2 = documentType == DocumentType.Acknowledgement
            ? relatedToMessageId1 // If Acknowledgement then related to message id must be the same to enable bundling.
            : MessageId.New();

        // - Create two message for same bundle
        var message1 = CreateMessage(documentType, receiver, relatedToMessageId1);
        var message2 = CreateMessage(documentType, receiver, relatedToMessageId2);

        // - Enqueue and bundle messages
        await EnqueueAndBundleMessages([message1, message2]);

        // Then message is added to the same bundle & bundle has correct count
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
    [MemberData(nameof(DocumentTypesForBundledMessageTypes))]
    public async Task Given_EnqueuedTwoMessageForDifferentReceivers_When_BundlingMessages_Then_TheABundleIsCreatedForEachMessage(
        DocumentType documentType)
    {
        // Given existing bundle
        var receiver1 = new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var receiver2 = new Actor(ActorNumber.Create("2222222222222"), ActorRole.EnergySupplier);
        var relatedToMessageId = MessageId.New(); // If Acknowledgement then related to message id must be the same to enable bundling.

        // - Create two message for same bundle
        var message1 = CreateMessage(documentType, receiver1, relatedToMessageId);
        var message2 = CreateMessage(documentType, receiver2, relatedToMessageId);

        // - Enqueue and bundle messages
        await EnqueueAndBundleMessages([message1, message2]);

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
    [MemberData(nameof(ReceiverActorRolesForNotifyValidatedMeasureData))]
    public async Task Given_EnqueuedNotifyValidatedMeasureDataForReceiver_When_BundlingMessages_Then_BundleIsCreatedWithCorrectValues(
        ActorRole receiverRole)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        var receiver = new Actor(ActorNumber.Create("1111111111111"), receiverRole);

        // - Create message for bundle
        var message = CreateMessage(DocumentType.NotifyValidatedMeasureData, receiver, relatedToMessageId: null);

        var bundleResult = await EnqueueAndBundleMessages([message]);

        // Then a bundle is created with correct values
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages.ToListAsync();
        var bundles = await dbContext.Bundles.ToListAsync();
        var actorMessageQueues = await dbContext.ActorMessageQueues.ToListAsync();

        // - One outgoing messages was enqueued
        // - One bundle was created
        Assert.Multiple(
            () => Assert.Single(outgoingMessages),
            () => Assert.Single(bundles));

        var bundle = bundles.Single();
        var outgoingMessage = outgoingMessages.Single();

        // - The outgoing message is assigned to the bundle
        // - The bundle is in the correct actor message queue (correct receiver)
        // - The bundle has correct values
        Assert.Multiple(
            () => Assert.Equal(outgoingMessage.AssignedBundleId, bundle.Id),
            () => Assert.Equal(Receiver.Create(receiver), actorMessageQueues.SingleOrDefault(amq => amq.Id == bundle.ActorMessageQueueId)?.Receiver),
            () => Assert.NotNull(bundle.RowVersion),
            () => Assert.Equal(bundleResult.BundlesCreatedAt, bundle.Created),
            () => Assert.Equal(bundleResult.BundlesCreatedAt, bundle.ClosedAt),
            () => Assert.True(bundle.IsClosed),
            () => Assert.Null(bundle.PeekedAt),
            () => Assert.Null(bundle.DequeuedAt),
            () => Assert.Equal(DocumentType.NotifyValidatedMeasureData, bundle.DocumentTypeInBundle),
            () => Assert.Equal(MessageCategory.MeasureData, bundle.MessageCategory),
            () => Assert.Equal(BusinessReason.PeriodicMetering, bundle.BusinessReason),
            () => Assert.Equal(bundlingOptions.MaxBundleMessageCount, bundle.MaxMessageCount),
            () => Assert.Null(bundle.RelatedToMessageId));
    }

    [Theory]
    [MemberData(nameof(ReceiverActorRolesForAcknowledgement))]
    public async Task Given_EnqueuedAcknowledgementForReceiver_When_BundlingMessages_ThenBundleIsCreatedWithCorrectValues(
        ActorRole receiverRole)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        var receiver = new Actor(ActorNumber.Create("1111111111111"), receiverRole);
        var relatedToMessageId = MessageId.New();

        // - Create message for bundle
        var message = CreateMessage(DocumentType.Acknowledgement, receiver, relatedToMessageId);

        // - Enqueue message "now"
        var (_, bundlesCreatedAt) = await EnqueueAndBundleMessages([message]);

        // Then a bundle is created with correct values
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages.ToListAsync();
        var bundles = await dbContext.Bundles.ToListAsync();
        var actorMessageQueues = await dbContext.ActorMessageQueues.ToListAsync();

        // - One outgoing messages was enqueued
        // - One bundle was created
        Assert.Multiple(
            () => Assert.Single(outgoingMessages),
            () => Assert.Single(bundles));

        var bundle = bundles.Single();
        var outgoingMessage = outgoingMessages.Single();

        // If MDR -> DDM hack is enabled, then the outgoing message is enqueued as MDR but should be added to the
        // DDM actor message queue.
        var expectedActorMessageQueueReceiver = WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack
                               && receiver.ActorRole == ActorRole.MeteredDataResponsible
            ? Receiver.Create(receiver.ActorNumber, ActorRole.GridAccessProvider)
            : Receiver.Create(receiver);

        // - The outgoing message is assigned to the bundle
        // - The bundle is in the correct actor message queue (correct receiver)
        // - The bundle has correct values
        Assert.Multiple(
            () => Assert.Equal(outgoingMessage.AssignedBundleId, bundle.Id),
            () => Assert.Equal(expectedActorMessageQueueReceiver, actorMessageQueues.SingleOrDefault(amq => amq.Id == bundle.ActorMessageQueueId)?.Receiver),
            () => Assert.NotNull(bundle.RowVersion),
            () => Assert.Equal(bundlesCreatedAt, bundle.Created),
            () => Assert.Equal(bundlesCreatedAt, bundle.ClosedAt),
            () => Assert.True(bundle.IsClosed),
            () => Assert.Null(bundle.PeekedAt),
            () => Assert.Null(bundle.DequeuedAt),
            () => Assert.Equal(DocumentType.Acknowledgement, bundle.DocumentTypeInBundle),
            () => Assert.Equal(MessageCategory.MeasureData, bundle.MessageCategory),
            () => Assert.Equal(BusinessReason.PeriodicMetering, bundle.BusinessReason),
            () => Assert.Equal(bundlingOptions.MaxBundleMessageCount, bundle.MaxMessageCount),
            () => Assert.Equal(outgoingMessage.RelatedToMessageId, bundle.RelatedToMessageId));
    }

    [Fact]
    public async Task Given_EnqueuedMessagesForTwoBundles_AndGiven_PartialBundleDoesNotHaveMessageOldEnoughToBeBundled_When_BundleMessages_Then_OnlyOneBundleIsCreatedWithCorrectCount()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var documentType = DocumentType.NotifyValidatedMeasureData;
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // - Create messages for two bundles
        var messagesForBundle1 = Enumerable.Range(0, bundlingOptions.MaxBundleMessageCount)
            .Select(_ => CreateMessage(documentType, receiver, relatedToMessageId: null))
            .ToList();
        var messageForPartialBundle = CreateMessage(documentType, receiver, relatedToMessageId: null);

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
            () => Assert.Equal(bundlingOptions.MaxBundleMessageCount + 1, outgoingMessages.Count),
            () => Assert.Single(outgoingMessages, om => om.AssignedBundleId == null),
            () => Assert.Collection(
                collection: bundles,
                elementInspectors: b => Assert.Equal(
                    expected: bundlingOptions.MaxBundleMessageCount,
                    actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id))));
    }

    [Fact]
    public async Task Given_EnqueuedMessagesForTwoBundles_AndGiven_PartialBundleHasMessageOldEnoughToBeBundled_When_BundleMessages_Then_TwoBundlesAreCreatedWithCorrectCount()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var documentType = DocumentType.NotifyValidatedMeasureData;
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // - Create messages for two bundles
        var messagesForBundle1 = Enumerable.Range(0, bundlingOptions.MaxBundleMessageCount)
            .Select(_ => CreateMessage(documentType, receiver, relatedToMessageId: null))
            .ToList();
        var message1ForPartialBundle = CreateMessage(documentType, receiver, relatedToMessageId: null);
        var message2ForPartialBundle = CreateMessage(documentType, receiver, relatedToMessageId: null);

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
            () => Assert.Equal(bundlingOptions.MaxBundleMessageCount + 2, outgoingMessages.Count),
            () => Assert.All(outgoingMessages, om => Assert.NotNull(om.AssignedBundleId)),
            () => Assert.Collection(
                collection: bundles.OrderByDescending(b => outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                elementInspectors:
                [
                    // First bundle has MaxBundleSize messages
                    b => Assert.Equal(
                        expected: bundlingOptions.MaxBundleMessageCount,
                        actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                    // Second bundle has 2 messages
                    b => Assert.Equal(
                        expected: 2,
                        actual: outgoingMessages.Count(om => om.AssignedBundleId == b.Id)),
                ]));
    }

    [Fact]
    public async Task Given_ManyMessagesEnqueuedForTwoDifferentReceivers_When_BundleMessages_Then_AllMessagesAreInCorrectBundles()
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
        var bundleSize = bundlingOptions.MaxBundleMessageCount; // Max bundle size = 2000
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
                        gridAreaCode: "804",
                        series: new ForwardMeasurementsMessageSeriesDto(
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

    [Theory]
    [InlineData(1, 2000, 2)] // 4*24 points * 1 day * 2000 messages = 192000 data points, should be in 2 bundles.
    [InlineData(31, 200, 4)] // 4*24 points * 31 days * 200 messages = 595200 data points, should be in 4 bundles.
    [InlineData(365, 20, 5)] // 4*24 points * 365 days * 20 messages = 700800 data points, should be in 5 bundles.
    public async Task Given_MessagesEnqueuedThatExceedMaxDataCount_When_BundleMessages_Then_AllMessagesAreInCorrectBundles(
        int periodDays,
        int enqueueMessageCount,
        int expectedBundleCount)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        if (enqueueMessageCount > bundlingOptions.MaxBundleMessageCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(enqueueMessageCount),
                enqueueMessageCount,
                "Enqueue message count must be less than or equal to max bundle message count, since this test is about max data count.");
        }

        var receiver = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var resolution = Resolution.QuarterHourly;
        var resolutionDuration = Duration.FromMinutes(15);

        // - Create actor message queue for receiver
        using (var setupScope = ServiceProvider.CreateScope())
        {
            var amqContext = setupScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(receiver));
            await amqContext.SaveChangesAsync();
        }

        // - Create messages for receiver
        var startDate = Instant.FromUtc(2024, 12, 31, 23, 00, 00);
        var endDate = startDate.Plus(Duration.FromDays(periodDays));

        var messagePeriod = new Interval(startDate, endDate);
        var measureDataForEachMessageCount = (int)Math.Ceiling(messagePeriod.Duration / resolutionDuration);
        var measureDataForEachMessage = Enumerable.Range(0, measureDataForEachMessageCount)
            .Select(i => new EnergyObservationDto(i + 1, decimal.MaxValue, Quality.Calculated))
            .ToList();

        var totalMeasureDataCount = (double)measureDataForEachMessageCount * enqueueMessageCount;

        var maxBundleDataCount = (double)bundlingOptions.MaxBundleDataCount;

        var messagesToEnqueue = Enumerable.Range(0, enqueueMessageCount)
            .Select(
                i => new AcceptedForwardMeteredDataMessageDto(
                    eventId: EventId.From(Guid.NewGuid()),
                    externalId: new ExternalId(Guid.NewGuid()),
                    receiver: receiver.ToActor(),
                    businessReason: BusinessReason.PeriodicMetering,
                    relatedToMessageId: MessageId.New(),
                    gridAreaCode: "804",
                    series: new ForwardMeasurementsMessageSeriesDto(
                        TransactionId: TransactionId.New(),
                        MarketEvaluationPointNumber: "1234567890123456",
                        MarketEvaluationPointType: MeteringPointType.Consumption,
                        OriginalTransactionIdReferenceId: TransactionId.New(),
                        Product: "1234567890123456",
                        QuantityMeasureUnit: MeasurementUnit.KilowattHour,
                        RegistrationDateTime: messagePeriod.Start,
                        Resolution: resolution,
                        StartedDateTime: messagePeriod.Start,
                        EndedDateTime: messagePeriod.End,
                        EnergyObservations: measureDataForEachMessage)))
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
            .GroupBy(om => om.AssignedBundleId)
            .ToDictionaryAsync(
                keySelector: om => om.Key ?? nullBundleKey,
                elementSelector: om => om.ToList());

        var bundles = await dbContext.Bundles
            .ToListAsync();

        _testOutputHelper.WriteLine(
            "Created {0} bundles (for {1} messages, {5} total measure data). Elapsed time: {2:D2}m{3:D2}s{4:D2}ms",
            bundles.Count,
            enqueuedMessagesCount,
            bundleStopwatch.Elapsed.Minutes,
            bundleStopwatch.Elapsed.Seconds,
            bundleStopwatch.Elapsed.Milliseconds,
            totalMeasureDataCount);

        Assert.Multiple(
            () => Assert.Equal(enqueuedMessagesCount, outgoingMessages.SelectMany(om => om.Value).Count()), // All messages are enqueued
            () => Assert.DoesNotContain(outgoingMessages, om => om.Key == nullBundleKey), // All messages are assigned a bundle
            () => Assert.Equal(expectedBundleCount, bundles.Count), // The created bundle count is as expected
            () => Assert.All(
                bundles,
                // The bundles data count does not exceed the max data count
                b =>
                {
                    var outgoingMessagesForBundle = outgoingMessages[b.Id];
                    var totalMeasureDataCountForBundle = outgoingMessagesForBundle.Sum(om => om.DataCount);
                    Assert.True(totalMeasureDataCountForBundle <= maxBundleDataCount, $"Bundle data count must be below max data count ({maxBundleDataCount}), but was {totalMeasureDataCount}");
                }));
    }

    /// <summary>
    /// If a message's data count exceeds the max data count, then it should be bundled in a separate bundle.
    /// </summary>
    [Fact]
    public async Task Given_TwoMessagesEnqueuedThatBothExceedMaxDataCount_When_BundleMessages_Then_2BundlesAreCreated()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;
        var countThatExceedsMaxBundleDataCount = bundlingOptions.MaxBundleDataCount + 1;

        var receiver = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);

        // - Create actor message queue for receiver
        using (var setupScope = ServiceProvider.CreateScope())
        {
            var amqContext = setupScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            amqContext.ActorMessageQueues.Add(ActorMessageQueue.CreateFor(receiver));
            await amqContext.SaveChangesAsync();
        }

        var measureDataForEachMessage = Enumerable.Range(0, count: countThatExceedsMaxBundleDataCount)
            .Select(i => new EnergyObservationDto(i + 1, decimal.MaxValue, Quality.Calculated))
            .ToList();

        var messagesToEnqueue = Enumerable.Range(start: 0, count: 2)
            .Select(
                _ => new AcceptedForwardMeteredDataMessageDto(
                    eventId: EventId.From(Guid.NewGuid()),
                    externalId: new ExternalId(Guid.NewGuid()),
                    receiver: receiver.ToActor(),
                    businessReason: BusinessReason.PeriodicMetering,
                    relatedToMessageId: MessageId.New(),
                    gridAreaCode: "804",
                    series: new ForwardMeasurementsMessageSeriesDto(
                        TransactionId: TransactionId.New(),
                        MarketEvaluationPointNumber: "1234567890123456",
                        MarketEvaluationPointType: MeteringPointType.Consumption,
                        OriginalTransactionIdReferenceId: TransactionId.New(),
                        Product: "1234567890123456",
                        QuantityMeasureUnit: MeasurementUnit.KilowattHour,
                        RegistrationDateTime: Instant.FromUtc(2024, 12, 31, 23, 00, 00),
                        Resolution: Resolution.QuarterHourly,
                        StartedDateTime: Instant.FromUtc(2024, 12, 31, 23, 00, 00),
                        EndedDateTime: Instant.FromUtc(2024, 12, 31, 23, 15, 00),
                        EnergyObservations: measureDataForEachMessage)))
            .Cast<OutgoingMessageDto>()
            .ToList();

        var totalMeasureDataCount = (double)measureDataForEachMessage.Count * messagesToEnqueue.Count;

        // - Set messages created at to "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);

        // - Enqueue messages for receiver
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
            .GroupBy(om => om.AssignedBundleId)
            .ToDictionaryAsync(
                keySelector: om => om.Key ?? nullBundleKey,
                elementSelector: om => om.ToList());

        var bundles = await dbContext.Bundles
            .ToListAsync();

        _testOutputHelper.WriteLine(
            "Created {0} bundles (for {1} messages, {5} total measure data). Elapsed time: {2:D2}m{3:D2}s{4:D2}ms",
            bundles.Count,
            enqueuedMessagesCount,
            bundleStopwatch.Elapsed.Minutes,
            bundleStopwatch.Elapsed.Seconds,
            bundleStopwatch.Elapsed.Milliseconds,
            totalMeasureDataCount);

        Assert.Multiple(
            () => Assert.Equal(enqueuedMessagesCount, outgoingMessages.SelectMany(om => om.Value).Count()), // All messages are enqueued
            () => Assert.DoesNotContain(outgoingMessages, om => om.Key == nullBundleKey), // All messages are assigned a bundle
            () => Assert.Equal(2, bundles.Count), // The created bundle count is as expected
            () => Assert.All(
                bundles,
                // Each bundle only contains a single outgoing message
                b =>
                {
                    var outgoingMessagesForBundle = outgoingMessages[b.Id];
                    Assert.Single(outgoingMessagesForBundle);
                }));
    }

    [Fact]
    public async Task Given_EnqueuedTwoAcknowledgementMessagesWithDifferentRelatedToMessageIds_When_BundlingMessages_Then_TheABundleIsCreatedForEachMessage()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // Given existing bundle
        var documentType = DocumentType.Acknowledgement;
        var receiver = new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var relatedToMessageId1 = MessageId.New();
        var relatedToMessageId2 = MessageId.New();

        // - Create actor message queue to avoid race condition that creates two actor message queues for the same receiver
        await CreateActorMessageQueueAndCommit(receiver);

        // - Create two message for same bundle
        var message1 = CreateMessage(documentType, receiver, relatedToMessageId1);
        var message2 = CreateMessage(documentType, receiver, relatedToMessageId2);

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

    private async Task CreateActorMessageQueueAndCommit(Actor actor)
    {
        var dbContext = ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var actorMessageQueue = ActorMessageQueue.CreateFor(Receiver.Create(actor));
        dbContext.ActorMessageQueues.Add(actorMessageQueue);
        await dbContext.SaveChangesAsync();
    }

    private OutgoingMessageDto CreateMessage(DocumentType documentType, Actor receiver, MessageId? relatedToMessageId)
    {
        return documentType switch
        {
            var dt when dt == DocumentType.NotifyValidatedMeasureData =>
                new AcceptedForwardMeteredDataMessageDtoBuilder()
                    .WithReceiver(receiver)
                    .Build(),

            var dt when dt == DocumentType.Acknowledgement =>
                new RejectedForwardMeteredDataMessageDtoBuilder()
                    .WithReceiver(receiver)
                    .WithDocumentReceiverRole(receiver.ActorRole)
                    .WithRelatedToMessageId(relatedToMessageId
                                            ?? throw new ArgumentNullException(
                                                paramName: nameof(relatedToMessageId),
                                                message: "RelatedToMessageId must be provided for Acknowledgement"))
                    .Build(),

            _ => throw new ArgumentOutOfRangeException(
                paramName: nameof(documentType),
                actualValue: documentType.Name,
                message: "Document type not supported."),
        };
    }

    private async Task<(Instant Now, Instant BundlesCreatedAt)> EnqueueAndBundleMessages(List<OutgoingMessageDto> messages)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        // - Enqueue messages "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);
        foreach (var message in messages)
        {
            await EnqueueAndCommitMessage(message, useNewScope: false);
        }

        // When creating bundles
        // - Move clock to when bundles should be created
        var whenBundlesShouldBeCreated = now.Plus(Duration.FromSeconds(bundlingOptions.BundleMessagesOlderThanSeconds));
        _clockStub.SetCurrentInstant(whenBundlesShouldBeCreated);

        // - Create bundles
        var bundleClient = ServiceProvider.GetRequiredService<IOutgoingMessagesBundleClient>();
        await bundleClient.BundleMessagesAndCommitAsync(CancellationToken.None);
        return (now, whenBundlesShouldBeCreated);
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
            RejectedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAsync(m, CancellationToken.None),
            _ => throw new NotImplementedException($"Enqueueing outgoing message of type {message.GetType()} is not implemented."),
        };

        await dbContext.SaveChangesAsync();

        scope?.Dispose();

        return messageId;
    }
}
