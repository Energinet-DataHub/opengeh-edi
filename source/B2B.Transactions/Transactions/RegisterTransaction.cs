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
using System.Threading.Tasks;
using B2B.Transactions.DataAccess;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.MessageHub.Model.Model;

namespace B2B.Transactions.Transactions
{
    public class RegisterTransaction
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMessageFactory<IMessage> _messageFactory;
        private readonly IOutbox _outbox;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterTransaction(IOutgoingMessageStore outgoingMessageStore, ITransactionRepository transactionRepository, IMessageFactory<IMessage> messageFactory, IOutbox outbox, IUnitOfWork unitOfWork)
        {
            _outgoingMessageStore = outgoingMessageStore ?? throw new ArgumentNullException(nameof(outgoingMessageStore));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
            _outbox = outbox;
            _unitOfWork = unitOfWork;
        }

        public Task HandleAsync(B2BTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);
            var outgoingMessage = new OutgoingMessage(_messageFactory.CreateMessage(transaction), transaction.Message.ReceiverId);

            _outgoingMessageStore.Add(outgoingMessage);

            //TODO: Insert correct values or fetch them later?
            //TODO: Get MessageType and documentType based on transaction.Message.ProcessType?
            var messageAvailable = new MessageAvailable(
                transaction.Message.MessageId,
                transaction.Message.ReceiverId,
                "ConfirmChangeOfSupplier",
                "MarketRoles",
                true,
                1,
                "ConfirmRequestChangeOfSupplier");

            _outbox.Add(messageAvailable);

            _unitOfWork.SaveTransaction();
            return Task.CompletedTask;
        }
    }

    #pragma warning disable
    public class OutgoingMessage
    {
        public OutgoingMessage(IMessage message, string recipientId)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            RecipientId = recipientId;
        }

        public IMessage Message { get; }
        public bool IsPublished { get; private set; }
        public string RecipientId { get; }
        public string MessageType => Message.MessageType;

        public void Published()
        {
            IsPublished = true;
        }
    }
    #pragma warning restore
}
