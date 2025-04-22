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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.OutgoingMessages.Bundling;

public class WhenPeekingMeasureDataWithBundlingTests : OutgoingMessagesTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ClockStub _clockStub;

    public WhenPeekingMeasureDataWithBundlingTests(
        OutgoingMessagesTestFixture outgoingMessagesTestFixture,
        ITestOutputHelper testOutputHelper)
        : base(outgoingMessagesTestFixture, testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _clockStub = (ClockStub)GetService<IClock>();
    }

    [Theory]
    [InlineData(1)] // A single measure data in each message
    [InlineData(16)] // 4 hour message (4*4=16)
    [InlineData(75)] // 75 measure data (150.000/2000) is the biggest possible bundle when using MaxBundleMessageCount=2000 and MaxBundleDataCount=150000
    [InlineData(96)] // 1 day (4*24=96)
    [InlineData(2976)] // 1 month (4*24*31=2976)
    [InlineData(35040)] // 1 year (4*24*365=35040)
    public async Task Given_EnqueuedRsm012BundleWithMaximumSize_When_PeekMessages_Then_BundleSizeIsBelow50MB(int dataCountForPeriod)
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        var receiver = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var eventId = EventId.From(Guid.NewGuid());

        var resolution = Resolution.QuarterHourly;
        var resolutionDuration = Duration.FromMinutes(15);
        var periodStart = Instant.FromUtc(2024, 12, 31, 23, 00, 00);
        var periodDuration = resolutionDuration * dataCountForPeriod;
        var periodEnd = periodStart.Plus(periodDuration);

        // var measureDataForPeriodCount = measureDataPeriod.Duration / resolutionDuration;
        var measureDataForPeriod = Enumerable.Range(0, dataCountForPeriod)
            .Select(i => new EnergyObservationDto(i + 1, decimal.MaxValue, Quality.Calculated))
            .ToList();

        var maxBundleMessageSize = bundlingOptions.MaxBundleMessageCount;
        var maxBundleDataCount = bundlingOptions.MaxBundleDataCount;

        // Create bundles for either maxBundleSize or the amount of bundles before the maxMeasureDataCount is exceeded
        var bundlesToCreateCount = Math.Min(maxBundleMessageSize, maxBundleDataCount / measureDataForPeriod.Count);
        var documentFormat = DocumentFormat.Ebix; // ebIX is the largest format for RSM-012

        var messagesToEnqueue = Enumerable.Range(0, bundlesToCreateCount)
            .Select(
                i => new AcceptedForwardMeteredDataMessageDto(
                    eventId: eventId,
                    externalId: new ExternalId(Guid.NewGuid()),
                    receiver: receiver.ToActor(),
                    businessReason: BusinessReason.PeriodicMetering,
                    relatedToMessageId: MessageId.New(),
                    series: new ForwardMeteredDataMessageSeriesDto(
                        TransactionId: TransactionId.New(),
                        MarketEvaluationPointNumber: "1234567890123456",
                        MarketEvaluationPointType: MeteringPointType.Consumption,
                        OriginalTransactionIdReferenceId: TransactionId.New(),
                        Product: "1234567890123456",
                        QuantityMeasureUnit: MeasurementUnit.KilowattHour,
                        RegistrationDateTime: periodEnd,
                        Resolution: resolution,
                        StartedDateTime: periodStart,
                        EndedDateTime: periodEnd,
                        EnergyObservations: measureDataForPeriod)))
            .Cast<OutgoingMessageDto>()
            .ToList();

        // - Set messages created at to "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);

        // - Create actor message queue for receiver
        await using (var arrangeScope = ServiceProvider.CreateAsyncScope())
        {
            var arrangeDbContext = arrangeScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            arrangeDbContext.ActorMessageQueues.Add(actorMessageQueue);
            await arrangeDbContext.SaveChangesAsync();
        }

        // - Enqueue messages for receivers
        var enqueueStopwatch = Stopwatch.StartNew();
        _testOutputHelper.WriteLine("Enqueueing {0} messages", messagesToEnqueue.Count);
        var enqueuedMessagesCount = await EnqueueAndCommitMessages(
            messagesToEnqueue: messagesToEnqueue);
        enqueueStopwatch.Stop();
        _testOutputHelper.WriteLine("Enqueued {0} messages in {1:F} seconds", enqueuedMessagesCount, enqueueStopwatch.Elapsed.TotalSeconds);

        Bundle biggestPossibleBundle;
        await using (var arrangeScope = ServiceProvider.CreateAsyncScope())
        {
            var arrangeDbContext = arrangeScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

            biggestPossibleBundle = new Bundle(
                actorMessageQueueId: actorMessageQueue.Id,
                businessReason: BusinessReason.PeriodicMetering,
                documentTypeInBundle: DocumentType.NotifyValidatedMeasureData,
                maxNumberOfMessagesInABundle: maxBundleMessageSize,
                created: now,
                relatedToMessageId: null);

            var outgoingMessagesForBundle = await arrangeDbContext.OutgoingMessages.ToListAsync();
            foreach (var outgoingMessageForBundle in outgoingMessagesForBundle)
            {
                biggestPossibleBundle.Add(outgoingMessageForBundle);
            }

            biggestPossibleBundle.Close(now);

            arrangeDbContext.Bundles.Add(biggestPossibleBundle);

            await arrangeDbContext.SaveChangesAsync();
        }

        // When peeking
        PeekResultDto? peekResult;
        var peekStopwatch = new Stopwatch();
        await using (var actScope = ServiceProvider.CreateAsyncScope())
        {
            var authenticatedActor = actScope.ServiceProvider.GetRequiredService<AuthenticatedActor>();
            authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
                actorNumber: receiver.Number,
                restriction: Restriction.None,
                actorRole: receiver.ActorRole,
                actorClientId: Guid.NewGuid(),
                actorId: Guid.NewGuid()));

            var peekMessages = actScope.ServiceProvider.GetRequiredService<PeekMessage>();

            _testOutputHelper.WriteLine("Peeking biggest possible bundle");
            peekStopwatch.Start();
            peekResult = await peekMessages.PeekAsync(
                new PeekRequestDto(
                    receiver.Number,
                    MessageCategory.MeasureData,
                    receiver.ActorRole,
                    documentFormat),
                CancellationToken.None);
            peekStopwatch.Stop();
        }

        _testOutputHelper.WriteLine("Peeked biggest possible bundle in {0:F} seconds", peekStopwatch.Elapsed.TotalSeconds);

        // Then message is below 50MB
        Assert.NotNull(peekResult);
        Assert.Equal(biggestPossibleBundle.MessageId, peekResult.MessageId);
        peekResult.Bundle.Seek(0, SeekOrigin.Begin);

        var messageSizeAsBytes = peekResult.Bundle.Length;
        var sizeInMegabytes = messageSizeAsBytes / (1024.0 * 1024.0);

        _testOutputHelper.WriteLine("Bundle size was {0:F}MB", sizeInMegabytes);

        Assert.True(sizeInMegabytes <= 50.0, $"The peeked message size should be below 50MB, but was {sizeInMegabytes:F1}MB");

        // -- Uncomment below to save the peeked bundle to c://temp, if you need to inspect it.
        // peekResult.Bundle.Seek(0, SeekOrigin.Begin);
        // var filePath = Path.Combine("C://", "temp", $"rsm-012-bundle-{documentFormat.Name.ToLower()}-{measureDataForPeriodCount}points-{bundlesToCreateCount}transactions.{(documentFormat == DocumentFormat.Json ? "json" : "xml")}");
        // var directoryPath = Path.GetDirectoryName(filePath)!;
        // if (!Directory.Exists(directoryPath))
        //     Directory.CreateDirectory(directoryPath);
        //
        // await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        // {
        //     await peekResult.Bundle.CopyToAsync(fs);
        // }
    }

    [Fact]
    public async Task Given_EnqueuedRsm009BundleWithMaximumSize_When_PeekMessages_Then_BundleSizeIsBelow50MB()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        var receiver = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.GridAccessProvider);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var eventId = EventId.From(Guid.NewGuid());
        var relatedToMessageId = MessageId.New();

        var maxBundleSize = bundlingOptions.MaxBundleMessageCount;
        var documentFormat = DocumentFormat.Json; // ebIX only writes one reject reason in each RSM-009, so we use CIM JSON instead.

        // We should usually not have more than a couple of reject reasons in each message, but we test with 50
        // to make sure we can handle a lot of reject reasons in a combined bundle (50 * 2000), without exceeding 50MB.
        const int rejectReasonsCount = 50;
        var rejectReasonsForEachMessage = Enumerable.Range(0, count: rejectReasonsCount)
            .Select(
                i => new RejectReason(
                    $"XX{i}",
                    "En lang fejlbesked der eksisterer på både dansk og engelsk, og indeholder en del tekst / a long error message that exists in both Danish and English, and contains a lot of text"))
            .ToList();

        var messagesToEnqueue = Enumerable.Range(0, maxBundleSize)
            .Select(
                i => new RejectedForwardMeteredDataMessageDto(
                    eventId: eventId,
                    externalId: new ExternalId(Guid.NewGuid()),
                    receiverNumber: receiver.Number,
                    receiverRole: receiver.ActorRole,
                    businessReason: BusinessReason.PeriodicMetering,
                    relatedToMessageId: relatedToMessageId,
                    documentReceiverRole: receiver.ActorRole,
                    series: new RejectedForwardMeteredDataSeries(
                        TransactionId: TransactionId.New(),
                        OriginalTransactionIdReference: TransactionId.New(),
                        RejectReasons: rejectReasonsForEachMessage)))
            .Cast<OutgoingMessageDto>()
            .ToList();

        // - Set messages created at to "now"
        var now = Instant.FromUtc(2025, 03, 26, 13, 37);
        _clockStub.SetCurrentInstant(now);

        // - Create actor message queue for receiver
        await using (var arrangeScope = ServiceProvider.CreateAsyncScope())
        {
            var arrangeDbContext = arrangeScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();
            arrangeDbContext.ActorMessageQueues.Add(actorMessageQueue);
            await arrangeDbContext.SaveChangesAsync();
        }

        // - Enqueue messages for receivers
        var enqueueStopwatch = Stopwatch.StartNew();
        _testOutputHelper.WriteLine("Enqueueing {0} messages", messagesToEnqueue.Count);
        var enqueuedMessagesCount = await EnqueueAndCommitMessages(
            messagesToEnqueue: messagesToEnqueue);
        enqueueStopwatch.Stop();
        _testOutputHelper.WriteLine("Enqueued {0} messages in {1:F} seconds", enqueuedMessagesCount, enqueueStopwatch.Elapsed.TotalSeconds);

        Bundle biggestPossibleBundle;
        await using (var arrangeScope = ServiceProvider.CreateAsyncScope())
        {
            var arrangeDbContext = arrangeScope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

            biggestPossibleBundle = new Bundle(
                actorMessageQueueId: actorMessageQueue.Id,
                businessReason: BusinessReason.PeriodicMetering,
                documentTypeInBundle: DocumentType.Acknowledgement,
                maxNumberOfMessagesInABundle: maxBundleSize,
                created: now,
                relatedToMessageId: relatedToMessageId);

            var outgoingMessagesForBundle = await arrangeDbContext.OutgoingMessages.ToListAsync();
            foreach (var outgoingMessageForBundle in outgoingMessagesForBundle)
            {
                biggestPossibleBundle.Add(outgoingMessageForBundle);
            }

            biggestPossibleBundle.Close(now);

            arrangeDbContext.Bundles.Add(biggestPossibleBundle);

            await arrangeDbContext.SaveChangesAsync();
        }

        // When peeking
        PeekResultDto? peekResult;
        var peekStopwatch = new Stopwatch();
        await using (var actScope = ServiceProvider.CreateAsyncScope())
        {
            var authenticatedActor = actScope.ServiceProvider.GetRequiredService<AuthenticatedActor>();
            authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
                actorNumber: receiver.Number,
                restriction: Restriction.None,
                actorRole: receiver.ActorRole,
                actorClientId: Guid.NewGuid(),
                actorId: Guid.NewGuid()));

            var peekMessages = actScope.ServiceProvider.GetRequiredService<PeekMessage>();

            _testOutputHelper.WriteLine("Peeking biggest possible bundle");
            peekStopwatch.Start();
            peekResult = await peekMessages.PeekAsync(
                new PeekRequestDto(
                    receiver.Number,
                    MessageCategory.MeasureData,
                    receiver.ActorRole,
                    documentFormat),
                CancellationToken.None);
            peekStopwatch.Stop();
        }

        _testOutputHelper.WriteLine("Peeked biggest possible bundle in {0:F} seconds", peekStopwatch.Elapsed.TotalSeconds);

        // Then message is below 50MB
        Assert.NotNull(peekResult);
        Assert.Equal(biggestPossibleBundle.MessageId, peekResult.MessageId);

        peekResult.Bundle.Seek(0, SeekOrigin.Begin);

        var messageSizeAsBytes = peekResult.Bundle.Length;
        var sizeInMegabytes = messageSizeAsBytes / (1024.0 * 1024.0);

        _testOutputHelper.WriteLine("Bundle size was {0:F}MB", sizeInMegabytes);

        Assert.True(sizeInMegabytes <= 50.0, $"The peeked message size should be below 50MB, but was {sizeInMegabytes:F1}MB");

        // Uncomment below to save the peeked bundle to c://temp, if you need to inspect it.
        // peekResult.Bundle.Seek(0, SeekOrigin.Begin);
        // var filePath = Path.Combine("C://", "temp", $"rsm-009-bundle-{documentFormat.Name.ToLower()}-{rejectReasonsForEachMessage.Count}reasons-{maxBundleSize}transactions.{(documentFormat == DocumentFormat.Json ? "json" : "xml")}");
        // var directoryPath = Path.GetDirectoryName(filePath)!;
        // if (!Directory.Exists(directoryPath))
        //     Directory.CreateDirectory(directoryPath);
        //
        // await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        // {
        //     await peekResult.Bundle.CopyToAsync(fs);
        // }
    }

    private async Task<int> EnqueueAndCommitMessages(List<OutgoingMessageDto> messagesToEnqueue)
    {
        using var scope = ServiceProvider.CreateScope();
        var outgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ActorMessageQueueContext>();

        var enqueuedMessagesCount = 0;
        foreach (var messageDto in messagesToEnqueue)
        {
            _ = messageDto switch
            {
                AcceptedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAsync(m, CancellationToken.None),
                RejectedForwardMeteredDataMessageDto m => await outgoingMessagesClient.EnqueueAsync(m, CancellationToken.None),
                _ => throw new NotImplementedException($"Enqueueing outgoing message of type {messageDto.GetType()} is not implemented."),
            };
            enqueuedMessagesCount++;
            if (enqueuedMessagesCount % 500 == 0)
                _testOutputHelper.WriteLine("Enqueued {0} messages", enqueuedMessagesCount);
        }

        await dbContext.SaveChangesAsync();

        return enqueuedMessagesCount;
    }
}
