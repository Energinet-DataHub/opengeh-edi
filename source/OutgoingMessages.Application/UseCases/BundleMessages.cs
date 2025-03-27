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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

public class BundleMessages(
    ILogger<BundleMessages> logger,
    IClock clock,
    IOutgoingMessageRepository outgoingMessageRepository,
    IServiceScopeFactory serviceScopeFactory)
{
    private readonly ILogger<BundleMessages> _logger = logger;
    private readonly IClock _clock = clock;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository = outgoingMessageRepository;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;

    /// <summary>
    /// Closes bundles that are ready to be closed in a single transaction, and returns the number of closed bundles.
    /// </summary>
    public async Task<int> BundleMessagesAsync(CancellationToken cancellationToken)
    {
        var messagesReadyToBeBundled = await _outgoingMessageRepository
            .GetBundleMetadataForMessagesReadyToBeBundledAsync(cancellationToken)
            .ConfigureAwait(false);

        // TODO: Handle RSM-009 and related to message id bundling
        foreach (var bundleMetadata in messagesReadyToBeBundled)
        {
            // TODO: This loop could instead add a message to a service bus, and create bundles async separately in
            // a service bus trigger. This would increase scaling, instead of creating all bundles in a single transaction.
            using var scope = _serviceScopeFactory.CreateScope();
            var bundlesToCreate = await BundleMessagesForAsync(
                    scope,
                    bundleMetadata,
                    cancellationToken)
                .ConfigureAwait(false);

            var bundleRepository = scope.ServiceProvider.GetRequiredService<IBundleRepository>();
            bundleRepository.Add(bundlesToCreate);

            var actorMessageQueueContext = scope.ServiceProvider.GetRequiredService<IActorMessageQueueContext>();
            await actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return messagesReadyToBeBundled.Count;
    }

    private async Task<List<Bundle>> BundleMessagesForAsync(
        IServiceScope scope,
        BundleMetadataDto bundleMetadataDto,
        CancellationToken cancellationToken)
    {
        var receiver = Receiver.Create(bundleMetadataDto.ReceiverNumber, bundleMetadataDto.ReceiverRole);
        var businessReason = BusinessReason.FromName(bundleMetadataDto.BusinessReason);

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
                receiver: receiver,
                businessReason: businessReason,
                documentType: bundleMetadataDto.DocumentType,
                relatedToMessageId: null,
                cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "Creating bundles for {OutgoingMessagesCount} messages for Actor: {ActorNumber}, ActorRole: {ActorRole}, DocumentType: {DocumentType}.",
            outgoingMessages.Count,
            receiver.Number.Value,
            receiver.ActorRole.Name,
            bundleMetadataDto.DocumentType.Name);

        var bundlesToCreate = new List<Bundle>();

        var outgoingMessagesList = new List<OutgoingMessage>(outgoingMessages.OrderBy(om => om.CreatedAt));
        while (outgoingMessagesList.Count > 0)
        {
            const int bundleSize = 2000;
            var outgoingMessagesForBundle = outgoingMessagesList
                .Take(bundleSize)
                .ToList();

            var bundle = CreateBundle(
                actorMessageQueueId,
                businessReason,
                bundleMetadataDto.DocumentType,
                relatedToMessageId: null);
            bundlesToCreate.Add(bundle);

            foreach (var outgoingMessage in outgoingMessagesForBundle)
            {
                outgoingMessage.AssignToBundle(bundle.Id);
            }

            bundle.Close(_clock.GetCurrentInstant());

            outgoingMessagesList.RemoveRange(0, outgoingMessagesForBundle.Count());
        }

        _logger.LogInformation(
            "Creating {BundleCount} bundles for Actor: {ActorNumber}, ActorRole: {ActorRole}, DocumentType: {DocumentType}.",
            bundlesToCreate.Count,
            receiver.Number.Value,
            receiver.ActorRole.Name,
            bundleMetadataDto.DocumentType.Name);

        return bundlesToCreate;
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
            var dt when dt == DocumentType.NotifyValidatedMeasureData => 2000, // TODO: Get from config
            var dt when dt == DocumentType.Acknowledgement => 2000, // TODO: Get from config
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
