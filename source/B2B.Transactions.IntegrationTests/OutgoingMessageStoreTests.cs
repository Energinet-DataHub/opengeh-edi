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
using B2B.Transactions.DataAccess;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Outgoing;
using Xunit;

namespace B2B.Transactions.IntegrationTests
{
    public class OutgoingMessageStoreTests : TestBase
    {
        private SystemDateTimeProviderStub _dateTimeProvider = new();
        private TestHelper _testHelper;

        public OutgoingMessageStoreTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _testHelper = new TestHelper(
                GetService<ITransactionRepository>(),
                GetService<IUnitOfWork>(),
                GetService<IOutbox>(),
                GetService<IOutgoingMessageStore>(),
                GetService<IMessageFactory<IMessage>>());
        }

        [Fact]
        public async Task Can_get_unpublished_message_from_messagestore()
        {
            var now = _dateTimeProvider.Now();
            _dateTimeProvider.SetNow(now);
            var transaction = TestHelper.CreateTransaction();
            await _testHelper.RegisterTransactionAsync(transaction).ConfigureAwait(false);

            var unpublished = await _testHelper
                .MessageStore
                .GetUnpublishedAsync()
                .ConfigureAwait(false);

            Assert.Single(unpublished);
        }
    }
}
