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

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Transactions.DataAccess;
using B2B.Transactions.Messages;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Outgoing;

namespace B2B.Transactions.IntegrationTests.Transactions
{
    internal class TransactionBuilder
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMessageFactory<IDocument> _messageFactory;

        public TransactionBuilder(
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IOutgoingMessageStore messageStore,
            IMessageFactory<IDocument> messageFactory)
        {
            _transactionRepository = transactionRepository;
            UnitOfWork = unitOfWork;
            MessageStore = messageStore;
            _messageFactory = messageFactory;
        }

        public IOutgoingMessageStore MessageStore { get; }

        public IUnitOfWork UnitOfWork { get; }

        public static XDocument CreateDocument(string payload)
        {
            return XDocument.Parse(payload);
        }

        public Task RegisterTransactionAsync(B2BTransaction transaction)
        {
            var useCase = new RegisterTransaction(MessageStore, _transactionRepository, _messageFactory, UnitOfWork);
            return useCase.HandleAsync(transaction);
        }

        internal static B2BTransaction CreateTransaction()
        {
            return B2BTransaction.Create(
                new MessageHeader("fake", "E03", "fake", "DDZ", "fake", "DDQ", "fake"),
                new MarketActivityRecord()
                {
                    BalanceResponsibleId = "fake",
                    Id = "fake",
                    ConsumerId = "fake",
                    ConsumerName = "fake",
                    EffectiveDate = "fake",
                    EnergySupplierId = "fake",
                    MarketEvaluationPointId = "fake",
                });
        }
    }
}
