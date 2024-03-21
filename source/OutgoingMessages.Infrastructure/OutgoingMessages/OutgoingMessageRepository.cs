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

using System;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;

public class OutgoingMessageRepository : IOutgoingMessageRepository
{
    private readonly ActorMessageQueueContext _context;
    private readonly IFileStorageClient _fileStorageClient;

    public OutgoingMessageRepository(ActorMessageQueueContext context, IFileStorageClient fileStorageClient)
    {
        _context = context;
        _fileStorageClient = fileStorageClient;
    }

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

    public async Task<OutgoingMessageBundle> GetAsync(BundleId bundleId)
    {
        var outgoingMessages = await _context.OutgoingMessages.Where(x => x.AssignedBundleId == bundleId)
            .ToListAsync()
            .ConfigureAwait(false);

        var downloadAndSetMessageRecordTasks = outgoingMessages.Select(DownloadAndSetMessageRecordAsync);

        await Task.WhenAll(downloadAndSetMessageRecordTasks).ConfigureAwait(false);

        // All messages in a bundle have the same meta data
        var firstMessage = outgoingMessages.First();

        return new OutgoingMessageBundle(
            firstMessage.DocumentType,
            firstMessage.Receiver,
            firstMessage.ProcessId,
            firstMessage.BusinessReason,
            firstMessage.SenderId,
            firstMessage.SenderRole,
            bundleId,
            outgoingMessages,
            firstMessage.RelatedToMessageId);
    }

    private async Task DownloadAndSetMessageRecordAsync(OutgoingMessage outgoingMessage)
    {
        var fileStorageFile = await _fileStorageClient.DownloadAsync(outgoingMessage.FileStorageReference).ConfigureAwait(false);

        var messageRecord = await fileStorageFile.ReadAsStringAsync().ConfigureAwait(false);

        outgoingMessage.SetSerializedContent(messageRecord);
    }
}
