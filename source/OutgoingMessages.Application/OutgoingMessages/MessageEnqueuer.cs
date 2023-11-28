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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class MessageEnqueuer : IMessageEnqueuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public MessageEnqueuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task EnqueueAsync(OutgoingMessageDto outgoingMessage)
    {
        if (outgoingMessage == null) throw new ArgumentNullException(nameof(outgoingMessage));

        var message = MapOutgoingMessage(outgoingMessage);

        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            message.Receiver.Number,
            message.Receiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            messageQueue = ActorMessageQueue.CreateFor(message.Receiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        messageQueue.Enqueue(message, _systemDateTimeProvider.Now());
        _outgoingMessageRepository.Add(message);
    }

    private static OutgoingMessage MapOutgoingMessage(OutgoingMessageDto messageDto)
    {
        return new OutgoingMessage(
            messageDto.DocumentType,
            messageDto.ReceiverId,
            messageDto.ProcessId,
            messageDto.BusinessReason,
            messageDto.ReceiverRole,
            messageDto.SenderId,
            messageDto.SenderRole,
            messageDto.MessageRecord);
    }
}
