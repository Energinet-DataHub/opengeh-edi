﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.OutgoingMessages;

public class OutgoingMessageRepository(
    IClock clock,
    IOptions<BundlingOptions> bundlingOptions,
    ActorMessageQueueContext context,
    IFileStorageClient fileStorageClient)
        : IOutgoingMessageRepository
{
    private readonly IClock _clock = clock;
    private readonly BundlingOptions _bundlingOptions = bundlingOptions.Value;
    private readonly ActorMessageQueueContext _context = context;
    private readonly IFileStorageClient _fileStorageClient = fileStorageClient;

    public async Task AddAsync(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Must await here to make sure the file is uploaded correctly before adding the outgoing message to the db context
        await _fileStorageClient.UploadAsync(
                message.FileStorageReference,
                message.GetSerializedContent())
            .ConfigureAwait(false);

        _context.OutgoingMessages.Add(message);
    }

    public async Task<OutgoingMessageBundle> GetAsync(PeekResult peekResult, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(peekResult);

        var outgoingMessages = await _context.OutgoingMessages
            .Where(x => x.AssignedBundleId == peekResult.BundleId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var downloadAndSetMessageRecordTasks = outgoingMessages
            .Select(x => DownloadAndSetMessageRecordAsync(x, cancellationToken));

        await Task.WhenAll(downloadAndSetMessageRecordTasks).ConfigureAwait(false);

        // All messages in a bundle have the same meta data
        var firstMessage = outgoingMessages.First();

        return new OutgoingMessageBundle(
            firstMessage.DocumentType,
            firstMessage.Receiver,
            firstMessage.DocumentReceiver,
            firstMessage.BusinessReason,
            firstMessage.SenderId,
            firstMessage.SenderRole,
            peekResult.MessageId,
            outgoingMessages,
            firstMessage.RelatedToMessageId);
    }

    public async Task<OutgoingMessage?> GetAsync(
        Receiver receiver,
        ExternalId externalId,
        CancellationToken cancellationToken = default)
    {
        return await _context.OutgoingMessages.FirstOrDefaultAsync(
            x => x.Receiver.Number == receiver.Number &&
                            x.Receiver.ActorRole == receiver.ActorRole &&
                            x.ExternalId == externalId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<OutgoingMessage?> GetAsync(Receiver receiver, ExternalId externalId, Instant? periodStartedAt)
    {
        return await _context.OutgoingMessages
            .FirstOrDefaultAsync(x =>
                x.Receiver.Number == receiver.Number &&
                x.Receiver.ActorRole == receiver.ActorRole &&
                x.ExternalId == externalId &&
                x.PeriodStartedAt == periodStartedAt)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<OutgoingMessage>> GetMessagesReadyToBeBundledAsync(CancellationToken cancellationToken)
    {
        var bundleMessagesCreatedBefore = _clock
            .GetCurrentInstant()
            .Minus(Duration.FromMinutes(_bundlingOptions.BundleDurationInMinutes));

        return await _context.OutgoingMessages
            .Where(
                om => om.AssignedBundleId == null &&
                      om.CreatedAt <= bundleMessagesCreatedBefore)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<OutgoingMessage>> GetMessagesForBundleAsync(
        Receiver receiver,
        string businessReason,
        DocumentType documentType,
        MessageId? relatedToMessageId,
        CancellationToken cancellationToken)
    {
        var query = _context.OutgoingMessages
            .Where(om => om.AssignedBundleId == null
                && om.DocumentType == documentType
                && om.BusinessReason == businessReason
                && om.Receiver == receiver);

        if (relatedToMessageId != null)
            query = query.Where(om => om.RelatedToMessageId == relatedToMessageId);

        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOutgoingMessagesIfExistsAsync(IReadOnlyCollection<BundleId> bundleMessageIds, CancellationToken cancellationToken)
    {
        var outgoingMessagesToBeRemoved = await _context.OutgoingMessages
            .Where(x => bundleMessageIds.Contains(x.AssignedBundleId))
            .ToListAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _context.RemoveRange(outgoingMessagesToBeRemoved);

        var fileStorageReferences = outgoingMessagesToBeRemoved
            .Select(x => x.FileStorageReference).ToList();
        await _fileStorageClient.DeleteIfExistsAsync(fileStorageReferences, FileStorageCategory.OutgoingMessage(), cancellationToken).ConfigureAwait(false);
    }

    private async Task DownloadAndSetMessageRecordAsync(OutgoingMessage outgoingMessage, CancellationToken cancellationToken)
    {
        var fileStorageFile = await _fileStorageClient
            .DownloadAsync(outgoingMessage.FileStorageReference, cancellationToken)
            .ConfigureAwait(false);

        var messageRecord = await fileStorageFile.ReadAsStringAsync().ConfigureAwait(false);

        outgoingMessage.SetSerializedContent(messageRecord);
    }
}
