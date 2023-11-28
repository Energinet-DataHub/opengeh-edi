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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessagesClient : IOutGoingMessagesClient
{
    private readonly MessagePeeker _messagePeeker;
    private readonly MessageDequeuer _messageDequeuer;
    private readonly IMessageEnqueuer _messageEnqueuer;

    public OutgoingMessagesClient(MessagePeeker messagePeeker, MessageDequeuer messageDequeuer, IMessageEnqueuer messageEnqueuer)
    {
        _messagePeeker = messagePeeker;
        _messageDequeuer = messageDequeuer;
        _messageEnqueuer = messageEnqueuer;
    }

    public async Task<DequeueRequestResult> DequeueAsync(DequeueRequestDto request)
    {
        return await _messageDequeuer.DequeueAsync(request).ConfigureAwait(false);
    }

    public async Task<PeekResult> PeekAsync(PeekRequest request, CancellationToken cancellationToken)
    {
        return await _messagePeeker.PeekAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueAsync(OutgoingMessageDto outgoingMessage)
    {
        await _messageEnqueuer.EnqueueAsync(outgoingMessage).ConfigureAwait(false);
    }
}
