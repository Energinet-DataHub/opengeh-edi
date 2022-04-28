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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using MarketActivityRecord = B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord;

namespace B2B.Transactions.OutgoingMessages
{
    public class MessageRequestHandler
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IncomingMessageStore _incomingMessageStore;
        private readonly IMessageDispatcher _messageDispatcherSpy;
        private readonly MessageFactory _messageFactory;

        public MessageRequestHandler(
            IOutgoingMessageStore outgoingMessageStore,
            IMessageDispatcher messageDispatcherSpy,
            MessageFactory messageFactory,
            IncomingMessageStore incomingMessageStore)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcherSpy = messageDispatcherSpy;
            _messageFactory = messageFactory;
            _incomingMessageStore = incomingMessageStore;
        }

        public async Task<Result> HandleAsync(IReadOnlyCollection<string> requestedMessageIds)
        {
            var messages = _outgoingMessageStore.GetByIds(requestedMessageIds);
            var exceptions = CheckBundleApplicability(requestedMessageIds, messages);
            if (exceptions.Count > 0)
            {
                return Result.Failure(exceptions.ToArray());
            }

            var message = await CreateMessageFromAsync(messages).ConfigureAwait(false);
            await _messageDispatcherSpy.DispatchAsync(message).ConfigureAwait(false);

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
            var expectedReceiver = messages.First().RecipientId;
            return messages.All(message => message.RecipientId.Equals(expectedReceiver, StringComparison.OrdinalIgnoreCase));
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

        private Task<Stream> CreateMessageFromAsync(ReadOnlyCollection<OutgoingMessage> outgoingMessages)
        {
            var incomingMessage = _incomingMessageStore.GetById(outgoingMessages[0].OriginalMessageId);
            var messageHeader = new MessageHeader(incomingMessage!.Message.ProcessType, incomingMessage.Message.ReceiverId, incomingMessage.Message.ReceiverRole, incomingMessage.Message.SenderId, incomingMessage.Message.SenderRole);
            var marketActivityRecords = new List<MarketActivityRecord>();
            foreach (var outgoingMessage in outgoingMessages)
            {
                marketActivityRecords.Add(
                    new MarketActivityRecord(outgoingMessage.Id.ToString(), incomingMessage.MarketActivityRecord.Id, incomingMessage.MarketActivityRecord.MarketEvaluationPointId));
            }

            return _messageFactory.CreateFromAsync(messageHeader, marketActivityRecords.AsReadOnly());
        }
    }
}
