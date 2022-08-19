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
using Messaging.Domain.SeedWork;

namespace Messaging.Application.OutgoingMessages.Requesting
{
    public class RequestMessagesHandler : IRequestHandler<RequestMessages, Unit>
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageRequestNotifications _messageRequestNotifications;
        private readonly DocumentFactory _documentFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMessageStorage _messageStorage;

        public RequestMessagesHandler(
            IOutgoingMessageStore outgoingMessageStore,
            IMessageRequestNotifications messageRequestNotificationsSpy,
            DocumentFactory documentFactory,
            ISystemDateTimeProvider systemDateTimeProvider,
            IMessageStorage messageStorage)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageRequestNotifications = messageRequestNotificationsSpy;
            _documentFactory = documentFactory;
            _systemDateTimeProvider = systemDateTimeProvider;
            _messageStorage = messageStorage;
        }

        public async Task<Unit> Handle(RequestMessages request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var requestedMessageIds = request.MessageIds.ToList();
            var messages = _outgoingMessageStore.GetByIds(requestedMessageIds);
            var messageIdsNotFound = MessageIdsNotFound(requestedMessageIds, messages);
            if (messageIdsNotFound.Any())
            {
                await _messageRequestNotifications.RequestedMessagesWasNotFoundAsync(messageIdsNotFound).ConfigureAwait(false);
                return Unit.Value;
            }

            var messageBundle = CreateBundleFrom(messages);

            var message = await _documentFactory.CreateFromAsync(messageBundle.CreateMessage(), EnumerationType.FromName<CimFormat>(request.RequestedDocumentFormat)).ConfigureAwait(false);
            var storedMessageLocation = await _messageStorage.SaveAsync(message).ConfigureAwait(false);
            await _messageRequestNotifications.SavedMessageSuccessfullyAsync(storedMessageLocation).ConfigureAwait(false);

            return Unit.Value;
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
