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

using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Transactions.DataAccess;
using B2B.Transactions.Messages;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Outgoing;

namespace B2B.Transactions.IntegrationTests
{
    public class TestHelper
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOutbox _outbox;
        private readonly IMessageFactory<IMessage> _messageFactory;

        public TestHelper(
            ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork,
            IOutbox outbox,
            IOutgoingMessageStore messageStore,
            IMessageFactory<IMessage> messageFactory)
        {
            _transactionRepository = transactionRepository;
            UnitOfWork = unitOfWork;
            _outbox = outbox;
            MessageStore = messageStore;
            _messageFactory = messageFactory;
        }

        public IOutgoingMessageStore MessageStore { get; }

        public IUnitOfWork UnitOfWork { get; }

        public static B2BTransaction CreateTransaction()
        {
            return B2BTransaction.Create(
                new MessageHeader("fake", "fake", "fake", "fake", "fake", "somedate", "fake"),
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

        public static XDocument CreateDocument(string payload)
        {
            return XDocument.Parse(payload);
        }

        public Task RegisterTransactionAsync(B2BTransaction transaction)
        {
            var useCase = new RegisterTransaction(MessageStore, _transactionRepository, _messageFactory, _outbox, UnitOfWork);
            return useCase.HandleAsync(transaction);
        }
    }
}
