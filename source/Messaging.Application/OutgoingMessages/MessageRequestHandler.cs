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
using System.Threading.Tasks;
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

        public MessageRequestHandler(
            IOutgoingMessageStore outgoingMessageStore,
            IMessageDispatcher messageDispatcherSpy,
            IUnitOfWork unitOfWork,
            MessageFactory messageFactory)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcher = messageDispatcherSpy;
            _unitOfWork = unitOfWork;
            _messageFactory = messageFactory;
        }

        public async Task<Result> HandleAsync(IReadOnlyCollection<string> requestedMessageIds)
        {
            var messages = _outgoingMessageStore.GetByIds(requestedMessageIds);
            var exceptions = CheckBundleApplicability(requestedMessageIds, messages);
            if (exceptions.Count > 0)
            {
                return Result.Failure(exceptions.ToArray());
            }

            var message = await _messageFactory.CreateFromAsync(messages).ConfigureAwait(false);
            await _messageDispatcher.DispatchAsync(message).ConfigureAwait(false);
            await _unitOfWork.CommitAsync().ConfigureAwait(false);

            return Result.Succeeded();
        }

        private static IReadOnlyList<Exception> CheckBundleApplicability(IReadOnlyCollection<string> requestedMessageIds, ReadOnlyCollection<OutgoingMessage> messages)
        {
            var exceptions = new List<Exception>();

            var messageIdsNotFound = MessageIdsNotFound(requestedMessageIds, messages);
            if (messageIdsNotFound.Any())
            {
                exceptions.AddRange(messageIdsNotFound
                    .Select(messageId => new OutgoingMessageNotFoundException(messageId))
                    .ToArray());
                return exceptions;
            }

            if (HasMatchingProcessTypes(messages) == false)
            {
                exceptions.Add(new ProcessTypesDoesNotMatchException(requestedMessageIds.ToArray()));
            }

            if (HasMatchingReceiver(messages) == false)
            {
                exceptions.Add(new ReceiverIdsDoesNotMatchException(requestedMessageIds.ToArray()));
            }

            return exceptions;
        }

        private static bool HasMatchingReceiver(IReadOnlyCollection<OutgoingMessage> messages)
        {
            var expectedReceiver = messages.First().ReceiverId;
            return messages.All(message => message.ReceiverId.Equals(expectedReceiver, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> MessageIdsNotFound(IReadOnlyCollection<string> requestedMessageIds, ReadOnlyCollection<OutgoingMessage> messages)
        {
            return requestedMessageIds
                .Except(messages.Select(message => message.Id.ToString()))
                .ToList();
        }

        private static bool HasMatchingProcessTypes(IReadOnlyCollection<OutgoingMessage> messages)
        {
            var expectedProcessType = messages.First().ProcessType;
            return messages.All(message => message.ProcessType.Equals(expectedProcessType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
