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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.Bundling;

public class WhenEnqueuingMeasureDataWithBundlingTests : OutgoingMessagesTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WhenEnqueuingMeasureDataWithBundlingTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
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
    public async Task Given_ExistingBundleForMessage_When_EnqueuingNewMessage_Then_MessageAddedToExistingBundle_AndThen_ExistingBundleHasCorrectCount(
        Func<Actor, OutgoingMessageDto> messageBuilder)
    {
        // Given existing bundle
        var receiver = new Actor(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);

        // Create existing message & bundle
        var existingMessage = messageBuilder(receiver);
        await EnqueueAndCommitMessageInNewScope(existingMessage);

        var messageToEnqueue = messageBuilder(receiver);

        // When enqueuing new message
        await EnqueueAndCommitMessageInNewScope(messageToEnqueue);

        // Then message is added to existing bundle & bundle has correct count
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .ToListAsync();

        Assert.Multiple(
            () => Assert.Equal(2, outgoingMessages.Count),
            () => Assert.Single(bundles),
            () => Assert.Collection(bundles, b => Assert.Equal(2, b.MessageCount)));

        var bundle = bundles.Single();
        Assert.All(outgoingMessages, om => Assert.Equal(bundle.Id, om.AssignedBundleId));
    }

    [Fact]
    public async Task Given_MultipleMessagesWithSameReceiverAndType_When_EnqueueingMessagesUsingParallel_Then_AllMessagesAreEnqueuedIn3Bundles()
    {
        var receiver = new Actor(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);

        var eventId = EventId.From(Guid.NewGuid());
        var startTime = Instant.FromUtc(2024, 03, 21, 23, 00, 00);
        const int messageCount = 5000;
        var messagesToEnqueue = Enumerable.Range(0, messageCount)
            .Select(
                i =>
                {
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
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

        var stopwatch = Stopwatch.StartNew();
        var counts = await EnqueueMessagesWithRecursiveRetries(
            messagesToEnqueue: messagesToEnqueue,
            cancellationToken: cancellationToken);

        _testOutputHelper.WriteLine("Test finished after enqueueing {0} ({1} retried, {4} total retries) messages. Elapsed time: {2:D2}m{3:D2}s", counts.EnqueuedCount, counts.RetriedCount, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds, counts.TotalRetries);

        // Then all messages are enqueued
        await using var scope = ServiceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
        var outgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();

        var bundles = await dbContext.Bundles
            .Where(b => b.MessageCategory == MessageCategory.MeasureData)
            .ToListAsync();

        Assert.Multiple(
            () => Assert.Equal(messageCount, outgoingMessages.Count),
            () => Assert.Equal(3, bundles.Count),
            () => Assert.All(
                bundles,
                b =>
                {
                    Assert.Multiple(
                        () => Assert.True(b.MessageCount <= b.MaxMessageCount, "MessageCount should be less than or equal to MaxMessageCount"),
                        // Message count matches the actual number of outgoing messages in the bundle
                        () => Assert.Equal(b.MessageCount, outgoingMessages.Count(om => om.AssignedBundleId == b.Id)));
                }));
    }

    private async Task<(int EnqueuedCount, int RetriedCount, int TotalRetries)> EnqueueMessagesWithRecursiveRetries(List<AcceptedForwardMeteredDataMessageDto> messagesToEnqueue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var enqueuedMessagesCount = 0;
        var retriedMessagesCount = 0;
        var totalRetries = 0;

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await Parallel.ForEachAsync(
                messagesToEnqueue,
                cancellationToken,
                async (m, _) =>
                {
                    var retryCount = await EnqueueAndCommitMessageWithRecursiveRetries(m, 0, cancellationToken);
                    enqueuedMessagesCount++;
                    if (retryCount > 0)
                    {
                        _testOutputHelper.WriteLine("Finished enqueueing message after {0} retries (enqueued {1} messages total, with {2} retries)", retryCount, enqueuedMessagesCount, retriedMessagesCount);
                        retriedMessagesCount++;
                        totalRetries += retryCount;
                    }

                    if (enqueuedMessagesCount % 500 == 0)
                        _testOutputHelper.WriteLine("[{0:D2}m{1:D2}s] Finished enqueueing {2} messages", stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds, enqueuedMessagesCount);
                });
        }
        catch (Exception e)
        {
            _testOutputHelper.WriteLine("Error enqueueing messages: {0}", e.InnerException?.Message ?? e.Message);
        }

        return (enqueuedMessagesCount, retriedMessagesCount, totalRetries);
    }

    private async Task<int> EnqueueAndCommitMessageWithRecursiveRetries(AcceptedForwardMeteredDataMessageDto message, int retryCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await EnqueueAndCommitMessageInNewScope(message);
        }
        catch (Exception)
        {
            retryCount++;
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            retryCount = await EnqueueAndCommitMessageWithRecursiveRetries(message, retryCount, cancellationToken);
        }

        return retryCount;
    }

    private async Task<Guid> EnqueueAndCommitMessageInNewScope(OutgoingMessageDto message)
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();

        var messageId = message switch
        {
            AcceptedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAsync(m, CancellationToken.None),
            RejectedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAndCommitAsync(m, CancellationToken.None),
            _ => throw new NotImplementedException($"Enqueueing outgoing message of type {message.GetType()} is not implemented."),
        };

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.CommitTransactionAsync(CancellationToken.None);

        return messageId;
    }
}
