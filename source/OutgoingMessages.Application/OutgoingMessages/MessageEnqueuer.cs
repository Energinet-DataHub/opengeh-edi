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
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;

public class MessageEnqueuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly IOutgoingMessageRepository _outgoingMessageRepository;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ILogger<MessageDequeuer> _logger;

    public MessageEnqueuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        IOutgoingMessageRepository outgoingMessageRepository,
        ISystemDateTimeProvider systemDateTimeProvider,
        ILogger<MessageDequeuer> logger)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _outgoingMessageRepository = outgoingMessageRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
        _logger = logger;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(OutgoingMessageDto messageToEnqueue)
    {
        if (messageToEnqueue == null) throw new ArgumentNullException(nameof(messageToEnqueue));

        var outgoingMessage = new OutgoingMessage(
            messageToEnqueue.DocumentType,
            messageToEnqueue.ReceiverId,
            messageToEnqueue.ProcessId,
            messageToEnqueue.BusinessReason,
            messageToEnqueue.ReceiverRole,
            messageToEnqueue.SenderId,
            messageToEnqueue.SenderRole,
            messageToEnqueue.MessageRecord,
            _systemDateTimeProvider.Now());

        var addToRepositoryTask = _outgoingMessageRepository.AddAsync(outgoingMessage);
        var addToActorMessageQueueTask = AddToActorMessageQueueAsync(outgoingMessage);

        await Task.WhenAll(addToRepositoryTask, addToActorMessageQueueTask).ConfigureAwait(false);
        _logger.LogInformation("Message enqueued: {Message} for Actor: {ActorNumber}", outgoingMessage.Id, outgoingMessage.Receiver.Number.Value);

        return outgoingMessage.Id;
    }

    private async Task AddToActorMessageQueueAsync(OutgoingMessage outgoingMessage)
    {
        var outgoingMessageReceiver = Receiver.Create(outgoingMessage.ReceiverId, outgoingMessage.ReceiverRole);
        var actorMessageQueue = await GetMessageQueueForReceiverAsync(outgoingMessageReceiver).ConfigureAwait(false);
        actorMessageQueue.Enqueue(outgoingMessage, _systemDateTimeProvider.Now());
    }

    private async Task<ActorMessageQueue> GetMessageQueueForReceiverAsync(Receiver receiver)
    {
        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            receiver.Number,
            receiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            _logger.LogInformation("Creating new message queue for Actor: {ActorNumber}, MarketRole: {MarketRole}", receiver.Number.Value, receiver.ActorRole.Name);
            messageQueue = ActorMessageQueue.CreateFor(receiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        return messageQueue;
    }
}
