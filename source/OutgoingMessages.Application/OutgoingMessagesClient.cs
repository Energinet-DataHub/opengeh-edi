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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessagesClient : IOutgoingMessagesClient
{
    private readonly MessagePeeker _messagePeeker;
    private readonly MessageDequeuer _messageDequeuer;
    private readonly MessageEnqueuer _messageEnqueuer;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;

    public OutgoingMessagesClient(
        MessagePeeker messagePeeker,
        MessageDequeuer messageDequeuer,
        MessageEnqueuer messageEnqueuer,
        ActorMessageQueueContext actorMessageQueueContext)
    {
        _messagePeeker = messagePeeker;
        _messageDequeuer = messageDequeuer;
        _messageEnqueuer = messageEnqueuer;
        _actorMessageQueueContext = actorMessageQueueContext;
    }

    public async Task<DequeueRequestResultDto> DequeueAndCommitAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        var dequeueRequestResult = await _messageDequeuer.DequeueAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dequeueRequestResult;
    }

    public async Task<PeekResultDto> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        var peekResult = await _messagePeeker.PeekAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return peekResult;
    }

    public async Task EnqueueAsync(OutgoingMessageDto outgoingMessage)
    {
        await _messageEnqueuer.EnqueueAsync(outgoingMessage).ConfigureAwait(false);
    }

    public async Task EnqueueAndCommitAsync(OutgoingMessageDto outgoingMessage, CancellationToken cancellationToken)
    {
        await _messageEnqueuer.EnqueueAsync(outgoingMessage).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
