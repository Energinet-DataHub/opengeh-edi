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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly IBundleStore _bundleStore;
    private readonly IMessageStorage _messageStorage;

    public PeekRequestHandler(
        ISystemDateTimeProvider systemDateTimeProvider,
        DocumentFactory documentFactory,
        IEnqueuedMessages enqueuedMessages,
        IBundleStore bundleStore,
        IMessageStorage messageStorage)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
        _documentFactory = documentFactory;
        _enqueuedMessages = enqueuedMessages;
        _bundleStore = bundleStore;
        _messageStorage = messageStorage;
    }

    public async Task<PeekResult> Handle(PeekRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = await _messageStorage
            .GetMessageOfAsync(BundleId.Create(request.MessageCategory, request.ActorNumber))
            .ConfigureAwait(false);

        if (document is not null)
        {
            var messageId = await _bundleStore
                .GetBundleMessageIdOfAsync(BundleId.Create(request.MessageCategory, request.ActorNumber))
                .ConfigureAwait(false);
            return new PeekResult(document, messageId);
        }

        var messages = await _enqueuedMessages.GetByAsync(request.ActorNumber, request.MessageCategory)
            .ConfigureAwait(false);

        if (messages is null)
        {
            return new PeekResult(null);
        }

        var bundle = new ReadyMessage(ReadyMessageId.New(), messages.Messages);

        var messageHeader = new MessageHeader(
            messages.ProcessType,
            messages.SenderNumber,
            messages.SenderRole,
            messages.ReceiverNumber,
            messages.ReceiverRole,
            bundle.Id.Value.ToString(),
            _systemDateTimeProvider.Now());
        var cimMessage = new CimMessage(
            messages.MessageType,
            messageHeader,
            messages.MessageRecords);

        document = await _documentFactory.CreateFromAsync(cimMessage, MessageFormat.Xml)
            .ConfigureAwait(false);
        bundle.SetGeneratedDocument(document);

        if (await _bundleStore.TryRegisterAsync(bundle)
                .ConfigureAwait(false) == false)
        {
            return new PeekResult(null);
        }

        return new PeekResult(document, bundle.Id.Value);
    }
}

public record PeekRequest(ActorNumber ActorNumber, MessageCategory MessageCategory) : ICommand<PeekResult>;

public record PeekResult(Stream? Bundle, Guid? MessageId = default);
