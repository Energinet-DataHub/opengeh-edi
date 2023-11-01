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

using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing;
using MediatR;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;

public class EnqueueMessageHandler : IRequestHandler<EnqueueMessageCommand>
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;

    public EnqueueMessageHandler(IActorMessageQueueRepository actorMessageQueueRepository)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
    }

    public async Task Handle(EnqueueMessageCommand request, CancellationToken cancellationToken)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var message = MapOutgoingMessage(request.OutgoingMessageDto);

        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            message.Receiver.Number,
            message.Receiver.ActorRole).ConfigureAwait(false);

        if (messageQueue == null)
        {
            messageQueue = ActorMessageQueue2.CreateFor(message.Receiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        messageQueue.Enqueue(message);
    }

    private OutgoingMessage MapOutgoingMessage(OutgoingMessageDto messageDto)
    {
        //TODO
        throw new NotImplementedException();
    }
}
