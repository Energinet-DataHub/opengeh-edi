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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages
{
    public class MessageRequestHandler
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MessageFactory _messageFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public MessageRequestHandler(
            IOutgoingMessageStore outgoingMessageStore,
            IMessageDispatcher messageDispatcherSpy,
            IUnitOfWork unitOfWork,
            MessageFactory messageFactory,
            ISystemDateTimeProvider systemDateTimeProvider)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcher = messageDispatcherSpy;
            _unitOfWork = unitOfWork;
            _messageFactory = messageFactory;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        public async Task<Result> HandleAsync(IReadOnlyCollection<string> requestedMessageIds)
        {
            var messages = _outgoingMessageStore.GetByIds(requestedMessageIds);
            var messageIdsNotFound = MessageIdsNotFound(requestedMessageIds, messages);
            if (messageIdsNotFound.Any())
            {
                return Result.Failure(new OutgoingMessageNotFoundException(messageIdsNotFound));
            }

            var bundle = new Bundle(_systemDateTimeProvider.Now());
            foreach (var outgoingMessage in messages)
            {
                bundle.Add(outgoingMessage);
            }

            var message = await _messageFactory.CreateFromAsync(bundle.CreateMessage()).ConfigureAwait(false);
            await _messageDispatcher.DispatchAsync(message).ConfigureAwait(false);
            await _unitOfWork.CommitAsync().ConfigureAwait(false);

            return Result.Succeeded();
        }

        private static List<string> MessageIdsNotFound(IReadOnlyCollection<string> requestedMessageIds, ReadOnlyCollection<OutgoingMessage> messages)
        {
            return requestedMessageIds
                .Except(messages.Select(message => message.Id.ToString()))
                .ToList();
        }
    }
}
