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
using B2B.Transactions.Configuration;
using B2B.Transactions.Configuration.DataAccess;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;

namespace B2B.Transactions.IncomingMessages
{
    public class IncomingMessageHandler
    {
        private readonly IncomingMessageStore _store;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICorrelationContext _correlationContext;

        public IncomingMessageHandler(IncomingMessageStore store, ITransactionRepository transactionRepository, IOutgoingMessageStore outgoingMessageStore, IUnitOfWork unitOfWork, ICorrelationContext correlationContext)
        {
            _store = store;
            _transactionRepository = transactionRepository;
            _outgoingMessageStore = outgoingMessageStore;
            _unitOfWork = unitOfWork;
            _correlationContext = correlationContext;
        }

        public Task HandleAsync(IncomingMessage incomingMessage)
        {
            if (incomingMessage == null) throw new ArgumentNullException(nameof(incomingMessage));
            _store.Add(incomingMessage);

            var acceptedTransaction = new AcceptedTransaction(incomingMessage.MarketActivityRecord.Id);
            _transactionRepository.Add(acceptedTransaction);

            var outgoingMessage = new OutgoingMessage(
                "ConfirmRequestChangeOfSupplier",
                incomingMessage.Message.SenderId,
                _correlationContext.Id,
                incomingMessage.Id,
                incomingMessage.Message.ProcessType,
                incomingMessage.Message.SenderRole,
                incomingMessage.Message.ReceiverId,
                incomingMessage.Message.ReceiverRole);
            _outgoingMessageStore.Add(outgoingMessage);

            return _unitOfWork.CommitAsync();
        }
    }
}
