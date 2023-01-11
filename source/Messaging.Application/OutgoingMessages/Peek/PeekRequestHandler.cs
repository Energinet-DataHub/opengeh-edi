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
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Commands.Commands;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

public class PeekRequestHandler : IRequestHandler<PeekRequest, PeekResult>
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly DocumentFactory _documentFactory;
    private readonly IEnqueuedMessages _enqueuedMessages;
    private readonly IReadyMessages _readyMessages;

    public PeekRequestHandler(
        ISystemDateTimeProvider systemDateTimeProvider,
        DocumentFactory documentFactory,
        IEnqueuedMessages enqueuedMessages,
        IReadyMessages readyMessages)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _enqueuedMessages = enqueuedMessages;
        _readyMessages = readyMessages;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var readyMessage = await _readyMessages.GetAsync(request.MessageCategory, request.ActorNumber).ConfigureAwait(false);
        if (readyMessage is not null)
        {
            return new PeekResult(readyMessage.GeneratedDocument, readyMessage.Id.Value);
        }

        var messageBundle = await _enqueuedMessages.GetByAsync(request.ActorNumber, request.MessageCategory)
            .ConfigureAwait(false);

        if (messageBundle is null)
        {
            return new PeekResult(null);
        }

        readyMessage = await CreateReadyMessageAsync(messageBundle).ConfigureAwait(false);

        if (await _readyMessages.TryAddAsync(readyMessage)
                .ConfigureAwait(false) == false)
        {
            return new PeekResult(null);
        }

        return new PeekResult(readyMessage.GeneratedDocument, readyMessage.Id.Value);
    }

    private async Task<BundledMessage> CreateReadyMessageAsync(MessageBundle messageBundle)
    {
        var id = BundledMessageId.New();
        var document = await _documentFactory.CreateFromAsync(id, messageBundle, MessageFormat.Xml, _systemDateTimeProvider.Now())
            .ConfigureAwait(false);
        return BundledMessage.CreateFrom(id, messageBundle, document);
    }
}

public record PeekRequest(ActorNumber ActorNumber, MessageCategory MessageCategory) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle, Guid? MessageId = default);
