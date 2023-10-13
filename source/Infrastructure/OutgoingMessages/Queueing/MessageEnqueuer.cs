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
using System.Diagnostics;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.Queueing;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Queueing;

public class MessageEnqueuer
{
    private readonly IActorMessageQueueRepository _actorMessageQueueRepository;
    private readonly ILogger<MessageEnqueuer> _logger;

    public MessageEnqueuer(
        IActorMessageQueueRepository actorMessageQueueRepository,
        ILogger<MessageEnqueuer> logger)
    {
        _actorMessageQueueRepository = actorMessageQueueRepository;
        _logger = logger;
    }

    public async Task EnqueueAsync(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var watch = new Stopwatch();
        watch.Start();
        _logger.LogInformation("Enqueue outgoing message \"{MessageId}\" for receiver \"{Receiver}\".", message.Id, message.Receiver);
        var messageQueue = await _actorMessageQueueRepository.ActorMessageQueueForAsync(
            message.Receiver.Number,
            message.Receiver.ActorRole).ConfigureAwait(false);

        watch.Stop();
        _logger.LogInformation("Outgoing message \"{MessageId}\" for receiver \"{Receiver}\" was enqueued successfully. Time elapsed: {TimeElapsed}", message.Id, message.Receiver, watch.Elapsed);
        if (messageQueue == null)
        {
            messageQueue = ActorMessageQueue.CreateFor(message.Receiver);
            await _actorMessageQueueRepository.AddAsync(messageQueue).ConfigureAwait(false);
        }

        messageQueue.Enqueue(message);
    }
}
