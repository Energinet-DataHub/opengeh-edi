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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;

namespace Messaging.Application.OutgoingMessages.MessageCount;

public class MessageCountRequestHandler : IRequestHandler<MessageCountRequest, MessageCountResult>
{
    private readonly IEnqueuedMessages _enqueuedMessages;

    public MessageCountRequestHandler(IEnqueuedMessages enqueuedMessages)
    {
        _enqueuedMessages = enqueuedMessages;
    }

    public async Task<MessageCountResult> Handle(MessageCountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var messageCount = await _enqueuedMessages.GetAvailableMessageCountAsync(request.ActorNumber).ConfigureAwait(false);
        return new MessageCountResult(messageCount);
    }
}

public record MessageCountRequest(ActorNumber ActorNumber) : ICommand<MessageCountResult>;

public record MessageCountResult(int MessageCount);
