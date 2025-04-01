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
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

public class BundleMessages(
    ILogger<BundleMessages> logger,
    TelemetryClient telemetryClient,
    IClock clock,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BundlingOptions> bundlingOptions,
    IOutgoingMessageRepository outgoingMessageRepository)
{
    private const string BundleSizeMetricName = "Bundle Size";
    private const string BundleTimespanSecondsMetricName = "Bundle Timespan (seconds)";
    private const string AverageMillisecondsBundlingDurationMetricName = "Average Bundling Duration (ms)";

    private readonly ILogger<BundleMessages> _logger = logger;
    private readonly TelemetryClient _telemetryClient = telemetryClient;
    private readonly IClock _clock = clock;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly BundlingOptions _bundlingOptions = bundlingOptions.Value;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository = outgoingMessageRepository;

    /// <summary>
    /// Closes bundles that are ready to be closed in a single transaction, and returns the number of closed bundles.
    /// </summary>
    public async Task BundleMessagesAndCommitAsync(CancellationToken cancellationToken)
    {
        var messagesReadyToBeBundled = await _outgoingMessageRepository
            .GetBundleMetadataForMessagesReadyToBeBundledAsync(cancellationToken)
            .ConfigureAwait(false);

        var bundleTasks = new List<Task>();
        foreach (var bundleMetadata in messagesReadyToBeBundled)
        {
            // TODO: This loop could instead add a message to a service bus, and create bundles async separately in
            // a service bus trigger. This would increase scaling, instead of creating all bundles in a single transaction.
            // Alternatively, it could be created as a DurableFunction, that first gets all bundle metadata, and then
            // fans out to create bundles for each bundle metadata in parallel.
            var bundleTask = BundleMessagesAndCommitAsync(bundleMetadata, cancellationToken);
            bundleTasks.Add(bundleTask);
        }

        await Task.WhenAll(bundleTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Bundle messages for a single receiver, and commit the bundles to the database.
    /// <remarks>This is done in a separate scope, to enable parallel bundling for different receivers.</remarks>
    /// </summary>
    /// <param name="bundleMetadata"></param>
    /// <param name="cancellationToken"></param>
    private async Task BundleMessagesAndCommitAsync(BundleMetadata bundleMetadata, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var bundlingStopwatch = Stopwatch.StartNew();
        var bundlesToCreate = await CreateBundlesAsync(
                scope,
                bundleMetadata,
                cancellationToken)
            .ConfigureAwait(false);

        var createdBundlesCount = bundlesToCreate.Count;
        _logger.LogInformation(
            "Creating {BundleCount} bundles (with {TotalMessageCount} messages) for Actor: {ActorNumber}, ActorRole: {ActorRole}, DocumentType: {DocumentType}, RelatedToMessageId: {RelatedToMessageId}.",
            createdBundlesCount,
            bundlesToCreate.Sum(b => b.MessageCount),
            bundleMetadata.ReceiverNumber.Value,
            bundleMetadata.ReceiverRole.Name,
            bundleMetadata.DocumentType.Name,
            bundleMetadata.RelatedToMessageId?.Value);

        var bundleRepository = scope.ServiceProvider.GetRequiredService<IBundleRepository>();
        bundleRepository.Add(bundlesToCreate.Select(b => b.Bundle).ToList());

        var actorMessageQueueContext = scope.ServiceProvider.GetRequiredService<IActorMessageQueueContext>();
        await actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        bundlingStopwatch.Stop();

        // Log bundle size & timespan metrics for document type
        var bundleSizeMetric = _telemetryClient.GetMetric(metricId: BundleSizeMetricName, dimension1Name: "DocumentType");
        var bundleTimespanSecondsMetric = _telemetryClient.GetMetric(metricId: BundleTimespanSecondsMetricName, dimension1Name: "DocumentType");
        bundlesToCreate.ForEach(b =>
        {
            bundleSizeMetric.TrackValue(metricValue: b.MessageCount, dimension1Value: bundleMetadata.DocumentType.Name);
            bundleTimespanSecondsMetric.TrackValue(metricValue: b.Timespan.TotalSeconds, dimension1Value: bundleMetadata.DocumentType.Name);
        });

        // Log average bundling duration for document type
        var averageBundlingDurationMetric = _telemetryClient.GetMetric(
            metricId: AverageMillisecondsBundlingDurationMetricName,
            dimension1Name: "DocumentType");
        averageBundlingDurationMetric.TrackValue(
            metricValue: (double)bundlingStopwatch.ElapsedMilliseconds / createdBundlesCount,
            dimension1Value: bundleMetadata.DocumentType.Name);
    }

    private async Task<List<(Bundle Bundle, int MessageCount, Duration Timespan)>> CreateBundlesAsync(
        IServiceScope scope,
        BundleMetadata bundleMetadata,
        CancellationToken cancellationToken)
    {
        var receiver = Receiver.Create(bundleMetadata.ReceiverNumber, bundleMetadata.ReceiverRole);
        var businessReason = BusinessReason.FromName(bundleMetadata.BusinessReason);

        var actorMessageQueueRepository = scope.ServiceProvider.GetRequiredService<IActorMessageQueueRepository>();

        var actorMessageQueueId = await GetActorMessageQueueIdForReceiverAsync(
                actorMessageQueueRepository,
                receiver,
                _logger,
                cancellationToken)
            .ConfigureAwait(false);

        var outgoingMessageRepository = scope.ServiceProvider.GetRequiredService<IOutgoingMessageRepository>();
        var outgoingMessages = await outgoingMessageRepository
            .GetMessagesForBundleAsync(
                receiver,
                businessReason,
                bundleMetadata.DocumentType,
                bundleMetadata.RelatedToMessageId,
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Creating bundles for {OutgoingMessagesCount} messages for Actor: {ActorNumber}, ActorRole: {ActorRole}, DocumentType: {DocumentType}, RelatedToMessageId: {RelatedToMessageId}.",
            outgoingMessages.Count,
            receiver.Number.Value,
            receiver.ActorRole.Name,
            bundleMetadata.DocumentType.Name,
            bundleMetadata.RelatedToMessageId?.Value);

        var bundlesToCreate = new List<(Bundle Bundle, int MessageCount, Duration Timespan)>();

        var bundleSize = _bundlingOptions.MaxBundleSize;
        var outgoingMessagesList = new List<OutgoingMessage>(outgoingMessages.OrderBy(om => om.CreatedAt));
        while (outgoingMessagesList.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var outgoingMessagesForBundle = outgoingMessagesList
                .Take(bundleSize)
                .ToList();

            // If the bundle isn't full, and no messages are older than the bundle duration, then do not create the bundle (yet).
            var isPartialBundle = outgoingMessagesForBundle.Count < bundleSize;
            if (isPartialBundle)
            {
                var anyMessageIsReadyToBeBundled = IsAnyMessageReadyToBeBundledYet(outgoingMessagesForBundle);
                if (!anyMessageIsReadyToBeBundled)
                    break;
            }

            var bundle = CreateAndCloseBundleForMessages(
                actorMessageQueueId: actorMessageQueueId,
                documentType: bundleMetadata.DocumentType,
                businessReason: businessReason,
                relatedToMessageId: bundleMetadata.RelatedToMessageId,
                outgoingMessagesForBundle: outgoingMessagesForBundle);

            var bundleTimespan =
                outgoingMessagesForBundle.Last().CreatedAt - outgoingMessagesForBundle.First().CreatedAt;

            bundlesToCreate.Add((bundle, outgoingMessagesForBundle.Count, bundleTimespan));

            outgoingMessagesList.RemoveRange(0, outgoingMessagesForBundle.Count);
        }

        return bundlesToCreate;
    }

    private Bundle CreateAndCloseBundleForMessages(
        ActorMessageQueueId actorMessageQueueId,
        DocumentType documentType,
        BusinessReason businessReason,
        MessageId? relatedToMessageId,
        List<OutgoingMessage> outgoingMessagesForBundle)
    {
        var bundle = CreateBundle(
            actorMessageQueueId,
            businessReason,
            documentType,
            relatedToMessageId);

        foreach (var outgoingMessage in outgoingMessagesForBundle)
        {
            bundle.Add(outgoingMessage);
        }

        bundle.Close(_clock.GetCurrentInstant());
        return bundle;
    }

    /// <summary>
    /// Check if any of the messages in the bundle is older enough to be bundled (yet).
    /// </summary>
    private bool IsAnyMessageReadyToBeBundledYet(List<OutgoingMessage> outgoingMessagesForBundle)
    {
        var bundleMessagesCreatedBefore = _clock
            .GetCurrentInstant()
            .Minus(Duration.FromSeconds(_bundlingOptions.BundleMessagesOlderThanSeconds));

        var anyMessageShouldBeBundledYet = outgoingMessagesForBundle.Any(om => om.CreatedAt <= bundleMessagesCreatedBefore);
        return anyMessageShouldBeBundledYet;
    }

    private async Task<ActorMessageQueueId> GetActorMessageQueueIdForReceiverAsync(
        IActorMessageQueueRepository actorMessageQueueRepository,
        Receiver receiver,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var actorMessageQueueId = await actorMessageQueueRepository.ActorMessageQueueIdForAsync(
            receiver.Number,
            receiver.ActorRole,
            cancellationToken).ConfigureAwait(false);

        if (actorMessageQueueId == null)
        {
            logger.LogInformation("Creating new message queue for Actor: {ActorNumber}, ActorRole: {ActorRole}", receiver.Number.Value, receiver.ActorRole.Name);
            var actorMessageQueueToCreate = ActorMessageQueue.CreateFor(receiver);
            actorMessageQueueRepository.Add(actorMessageQueueToCreate);
            actorMessageQueueId = actorMessageQueueToCreate.Id;
        }

        return actorMessageQueueId;
    }

    private Bundle CreateBundle(
        ActorMessageQueueId actorMessageQueueId,
        BusinessReason businessReason,
        DocumentType documentType,
        MessageId? relatedToMessageId)
    {
        var maxBundleSize = documentType switch
        {
            var dt when dt == DocumentType.NotifyValidatedMeasureData => _bundlingOptions.MaxBundleSize,
            var dt when dt == DocumentType.Acknowledgement => _bundlingOptions.MaxBundleSize,
            _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Document type doesn't support bundling."),
        };

        var newBundle = new Bundle(
            actorMessageQueueId: actorMessageQueueId,
            businessReason: businessReason,
            documentTypeInBundle: documentType,
            maxNumberOfMessagesInABundle: maxBundleSize,
            created: _clock.GetCurrentInstant(),
            relatedToMessageId: relatedToMessageId);

        return newBundle;
    }
}
