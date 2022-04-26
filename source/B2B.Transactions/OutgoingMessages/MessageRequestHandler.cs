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
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using MarketActivityRecord = B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier.MarketActivityRecord;

namespace B2B.Transactions.OutgoingMessages
{
    public class MessageRequestHandler
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IncomingMessageStore _incomingMessageStore;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly MessageFactory _messageFactory;

        public MessageRequestHandler(
            IOutgoingMessageStore outgoingMessageStore,
            MessageDispatcher messageDispatcher,
            MessageFactory messageFactory,
            IncomingMessageStore incomingMessageStore)
        {
            _outgoingMessageStore = outgoingMessageStore;
            _messageDispatcher = messageDispatcher;
            _messageFactory = messageFactory;
            _incomingMessageStore = incomingMessageStore;
        }

        public async Task<Result> HandleAsync(ReadOnlyCollection<string> messageIdsToForward)
        {
            var messages = _outgoingMessageStore.GetByIds(messageIdsToForward);
            var exceptions = EnsureMessagesExists(messageIdsToForward, messages);

            if (exceptions.Any())
            {
                return Result.Failure(exceptions);
            }

            var incomingMessage = _incomingMessageStore.GetById(messages[0].OriginalMessageId);
            var messageHeader = new MessageHeader(incomingMessage!.Message.ProcessType, incomingMessage.Message.ReceiverId, incomingMessage.Message.ReceiverRole, incomingMessage.Message.SenderId, incomingMessage.Message.SenderRole);

            var marketActivityRecords = new List<MarketActivityRecord>();
            foreach (var outgoingMessage in messages)
            {
                marketActivityRecords.Add(
                    new MarketActivityRecord(outgoingMessage.Id.ToString(), incomingMessage.MarketActivityRecord.Id, incomingMessage.MarketActivityRecord.MarketEvaluationPointId));
            }

            var message = await _messageFactory.CreateFromAsync(messageHeader, marketActivityRecords.AsReadOnly()).ConfigureAwait(false);
            await _messageDispatcher.DispatchAsync(message).ConfigureAwait(false);

            return Result.Succeeded();
        }

        private static List<OutgoingMessageNotFoundException> EnsureMessagesExists(ReadOnlyCollection<string> messageIdsToForward, ReadOnlyCollection<OutgoingMessage> messages)
        {
            return messageIdsToForward
                .Except(messages.Select(message => message.Id.ToString()))
                .Select(messageId => new OutgoingMessageNotFoundException(messageId))
                .ToList();
        }
    }
}
