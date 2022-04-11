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
using System.Threading.Tasks;
using B2B.Transactions.DataAccess;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Outgoing;

namespace B2B.Transactions.Transactions
{
    public class RegisterTransaction
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMessageFactory<IDocument> _messageFactory;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterTransaction(IOutgoingMessageStore outgoingMessageStore, ITransactionRepository transactionRepository, IMessageFactory<IDocument> messageFactory, IUnitOfWork unitOfWork)
        {
            _outgoingMessageStore = outgoingMessageStore ?? throw new ArgumentNullException(nameof(outgoingMessageStore));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
            _unitOfWork = unitOfWork;
        }

        public Task HandleAsync(B2BTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            var acceptedTransaction = new AcceptedTransaction(transaction.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage = new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId);

            _outgoingMessageStore.Add(outgoingMessage);

            return _unitOfWork.CommitAsync();
        }
    }
}
