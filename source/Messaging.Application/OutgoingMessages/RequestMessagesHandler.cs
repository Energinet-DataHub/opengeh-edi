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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages
{
    public class RequestMessagesHandler : IRequestHandler<RequestMessages, Result>
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly MessageFactory _messageFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public RequestMessagesHandler(
            IOutgoingMessageStore outgoingMessageStore,
            IMessageDispatcher messageDispatcherSpy,
            MessageFactory messageFactory,
            ISystemDateTimeProvider systemDateTimeProvider)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcher = messageDispatcherSpy;
            _messageFactory = messageFactory;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        public async Task<Result> Handle(RequestMessages request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var requestedMessageIds = request.MessageIds.ToList();
            var messages = _outgoingMessageStore.GetByIds(requestedMessageIds);
            var messageIdsNotFound = MessageIdsNotFound(requestedMessageIds, messages);
            if (messageIdsNotFound.Any())
            {
                throw new OutgoingMessageNotFoundException(messageIdsNotFound);
            }

            var messageBundle = CreateBundleFrom(messages);

            var message = await _messageFactory.CreateFromAsync(messageBundle.CreateMessage()).ConfigureAwait(false);
            await _messageDispatcher.DispatchAsync(message).ConfigureAwait(false);

            return Result.Succeeded();
        }

        private static List<string> MessageIdsNotFound(IReadOnlyCollection<string> requestedMessageIds, ReadOnlyCollection<OutgoingMessage> messages)
        {
            return requestedMessageIds
                .Except(messages.Select(message => message.Id.ToString()))
                .ToList();
        }

        private Bundle CreateBundleFrom(IReadOnlyList<OutgoingMessage> messages)
        {
            var bundle = new Bundle(_systemDateTimeProvider.Now());
            foreach (var outgoingMessage in messages)
            {
                bundle.Add(outgoingMessage);
            }

            return bundle;
        }
    }
}
