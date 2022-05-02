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
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.Transactions;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.IncomingMessages
{
    [IntegrationTest]
    public class IncomingMessageHandlingTests : TestBase
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IncomingMessageHandler _incomingMessageHandler;

        public IncomingMessageHandlingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _transactionRepository =
                GetService<ITransactionRepository>();
            _incomingMessageHandler = GetService<IncomingMessageHandler>();
        }

        [Fact]
        public async Task Transaction_is_registered()
        {
            var incomingMessage = IncomingMessageBuilder.CreateMessage();

            await _incomingMessageHandler.HandleAsync(incomingMessage).ConfigureAwait(false);

            var savedTransaction = _transactionRepository.GetById(incomingMessage.MarketActivityRecord.Id);
            Assert.NotNull(savedTransaction);
        }
    }
}
