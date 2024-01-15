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
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class MessageEnqueuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IOutgoingMessageFileStorage _outgoingMessageFileStorage;

    public MessageEnqueuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISystemDateTimeProvider systemDateTimeProvider,
        IOutgoingMessageFileStorage outgoingMessageFileStorage)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
        _outgoingMessageFileStorage = outgoingMessageFileStorage;
    }

    public async Task EnqueueAsync(OutgoingMessageDto messageToEnqueue)
    {
        if (messageToEnqueue == null) throw new ArgumentNullException(nameof(messageToEnqueue));

        var messageId = Guid.NewGuid();
        var messageReceiver = Receiver.Create(messageToEnqueue.ReceiverId, messageToEnqueue.ReceiverRole);

        using var messageAsStream = CreateStream(messageToEnqueue.MessageRecord);
        var timestamp = _systemDateTimeProvider.Now();
        var uploadFileTask = _outgoingMessageFileStorage.UploadAsync(
            messageAsStream,
            messageReceiver.Number,
            messageId,
            timestamp);

        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            messageReceiver.Number,
            messageReceiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            messageQueue = ActorMessageQueue.CreateFor(messageReceiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        var fileStorageReference = await uploadFileTask.ConfigureAwait(false);

        var outgoingMessage = new OutgoingMessage(
            messageId,
            messageToEnqueue.DocumentType,
            messageReceiver.Number,
            messageToEnqueue.ProcessId,
            messageToEnqueue.BusinessReason,
            messageReceiver.ActorRole,
            messageToEnqueue.SenderId,
            messageToEnqueue.SenderRole,
            messageToEnqueue.MessageRecord,
            fileStorageReference);

        messageQueue.Enqueue(outgoingMessage, _systemDateTimeProvider.Now());
        _outgoingMessageRepository.Add(outgoingMessage);
    }

    private static Stream CreateStream(string message)
    {
        var stream = new MemoryStream();
#pragma warning disable CA2000
        // Is disposed when the MemoryStream is disposed
        var writer = new StreamWriter(stream);
#pragma warning restore CA2000
        writer.Write(message);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
