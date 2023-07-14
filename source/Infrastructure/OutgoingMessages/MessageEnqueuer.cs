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
using Application.OutgoingMessages;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.Queueing;

namespace Infrastructure.OutgoingMessages;

public class MessageEnqueuer
{
    private readonly IOutgoingMessageStore _outgoingMessageStore;
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;

    public MessageEnqueuer(IOutgoingMessageStore outgoingMessageStore, IActorMessageQueueRepository actorMessageQueueRepository)
    {
        _outgoingMessageStore = outgoingMessageStore;
        _actorMessageQueueRepository = actorMessageQueueRepository;
    }

    public async Task EnqueueAsync(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(message.Receiver.Number, message.Receiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            messageQueue = ActorMessageQueue.CreateFor(message.Receiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        messageQueue.Enqueue(message);

        _outgoingMessageStore.Add(message);
    }
}
