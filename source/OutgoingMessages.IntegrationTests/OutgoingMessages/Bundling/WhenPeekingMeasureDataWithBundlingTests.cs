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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
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
using Energinet.DataHub.EDI.Tests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit.Abstractions;
using Period = NodaTime.Period;

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

    [Fact]
    public async Task Given_EnqueuedBundleWithMaximumSize_When_PeekMessages_Then_BundleSizeIsBelow50MB()
    {
        var bundlingOptions = ServiceProvider.GetRequiredService<IOptions<BundlingOptions>>().Value;

        var receiver = Receiver.Create(ActorNumber.Create("1111111111111"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var eventId = EventId.From(Guid.NewGuid());

        // Max measure data period is one year
        var longestMeasureDataPeriod = new Interval(
            Instant.FromUtc(2024, 12, 31, 23, 00, 00),
            // Instant.FromUtc(2025, 01, 01, 23, 00, 00)); // One day
            Instant.FromUtc(2025, 01, 31, 23, 00, 00)); // One month
            // Instant.FromUtc(2025, 12, 31, 23, 00, 00)); // One year

        var resolution = Resolution.QuarterHourly;
        var resolutionDuration = Duration.FromMinutes(15);
        var measureDataForLongestPeriodCount = longestMeasureDataPeriod.Duration / resolutionDuration;
        var measureDataForLongestPeriod = Enumerable.Range(0, (int)Math.Ceiling(measureDataForLongestPeriodCount))
            .Select(i => new EnergyObservationDto(i + 1, decimal.MaxValue, Quality.Calculated)) // TODO: What is the highest expected quantity value?
            .ToList();

        // var maxBundleSize = bundlingOptions.MaxBundleSize; // Max bundle size = 2000
        const int maxBundleSize = 100;
        var documentFormat = DocumentFormat.Ebix;

        var messagesToEnqueue = Enumerable.Range(0, maxBundleSize)
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
                        RegistrationDateTime: longestMeasureDataPeriod.Start,
                        Resolution: resolution,
                        StartedDateTime: longestMeasureDataPeriod.Start,
                        EndedDateTime: longestMeasureDataPeriod.End,
                        EnergyObservations: measureDataForLongestPeriod)))
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
                maxNumberOfMessagesInABundle: maxBundleSize,
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
        var filePath = Path.Combine("C://", "temp", $"bundle-{documentFormat.Name.ToLower()}-{measureDataForLongestPeriodCount}points-{maxBundleSize}transactions.{(documentFormat == DocumentFormat.Json ? "json" : "xml")}");
        var directoryPath = Path.GetDirectoryName(filePath)!;
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await peekResult.Bundle.CopyToAsync(fs);
        }

        peekResult.Bundle.Seek(0, SeekOrigin.Begin);

        var messageSizeAsBytes = peekResult.Bundle.Length;
        var sizeInMegabytes = messageSizeAsBytes / (1024.0 * 1024.0);

        _testOutputHelper.WriteLine("Bundle size was {0:F}MB", sizeInMegabytes);

        Assert.True(sizeInMegabytes <= 50.0, $"The peeked message size should be below 50MB, but was {sizeInMegabytes:F1}MB");
    }

    private async Task<int> EnqueueAndCommitMessages(List<OutgoingMessageDto> messagesToEnqueue)
    {
        var enqueuedMessagesCount = 0;
        var enqueueTasks = messagesToEnqueue
            .Select(async m =>
            {
                await EnqueueAndCommitMessage(m, useNewScope: true);
                enqueuedMessagesCount++;

                if (enqueuedMessagesCount % 100 == 0)
                    _testOutputHelper.WriteLine("Enqueued {0} messages", enqueuedMessagesCount);
            })
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
