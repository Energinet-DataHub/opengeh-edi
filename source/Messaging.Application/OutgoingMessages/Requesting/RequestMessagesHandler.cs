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
            IMessageRequestNotifications messageRequestNotifications,
            DocumentFactory documentFactory,
            ISystemDateTimeProvider systemDateTimeProvider,
            IMessageStorage messageStorage)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageRequestNotifications = messageRequestNotifications;
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
                await _messageRequestNotifications.RequestedMessagesWasNotFoundAsync(messageIdsNotFound, ParseRequestDetailsFrom(request)).ConfigureAwait(false);
                return Unit.Value;
            }

            var requestedFormat = EnumerationType.FromName<CimFormat>(request.RequestedDocumentFormat);
            var messageBundle = CreateBundleFrom(messages);
            var message = messageBundle.CreateMessage();

            if (_documentFactory.CanHandle(message.DocumentType, requestedFormat) == false)
            {
                await _messageRequestNotifications.RequestedDocumentFormatIsNotSupportedAsync(request.RequestedDocumentFormat, message.DocumentType, ParseRequestDetailsFrom(request)).ConfigureAwait(false);
                return Unit.Value;
            }

            await SaveDocumentAsync(message, requestedFormat, ParseRequestDetailsFrom(request)).ConfigureAwait(false);

            return Unit.Value;
        }

        private static MessageRequest ParseRequestDetailsFrom(RequestMessages request)
        {
            return new MessageRequest(
                request.RequestId,
                request.IdempotencyId,
                request.ReferenceId,
                request.DocumentType,
                request.RequestedDocumentFormat);
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

        private async Task SaveDocumentAsync(CimMessage message, CimFormat requestedFormat, MessageRequest request)
        {
            var document = await _documentFactory.CreateFromAsync(message, requestedFormat).ConfigureAwait(false);
            var storedMessageLocation = await _messageStorage.SaveAsync(document, request).ConfigureAwait(false);
            await _messageRequestNotifications.SavedMessageSuccessfullyAsync(storedMessageLocation, request).ConfigureAwait(false);
        }
    }
}
