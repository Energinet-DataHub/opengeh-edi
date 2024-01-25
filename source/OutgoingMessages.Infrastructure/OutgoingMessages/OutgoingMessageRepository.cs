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
using System.IO;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;

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

        _context.OutgoingMessages.Add(message);

        // Must await here instead of returning the Task, since messageRecordStream gets disposed when returning from function
        await _fileStorageClient.UploadAsync(
                message.FileStorageReference,
                message.GetMessageRecord())
            .ConfigureAwait(false);
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
            firstMessage.ReceiverId,
            firstMessage.ProcessId,
            firstMessage.BusinessReason,
            firstMessage.ReceiverRole,
            firstMessage.SenderId,
            firstMessage.SenderRole,
            firstMessage.IsPublished,
            bundleId,
            outgoingMessages);
    }

    private static async Task<string> ConvertToStringAsync(Stream stream)
    {
        using var streamReader = new StreamReader(stream);

        stream.Position = 0; // Make sure we read the entire stream
        var convertedToString = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        return convertedToString;
    }

    private async Task DownloadAndSetMessageRecordAsync(OutgoingMessage outgoingMessage)
    {
        var messageRecordStream = await _fileStorageClient.DownloadAsync(outgoingMessage.FileStorageReference).ConfigureAwait(false);

        var messageRecord = await ConvertToStringAsync(messageRecordStream).ConfigureAwait(false);

        outgoingMessage.SetMessageRecord(messageRecord);
    }
}
