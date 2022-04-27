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
using B2B.Transactions.Configuration;
using B2B.Transactions.Configuration.DataAccess;
using B2B.Transactions.Infrastructure.OutgoingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Incoming;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MessageHub.Model.Model;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.Infrastructure.OutgoingMessages
{
    [IntegrationTest]
    public class MessagePublisherTests : TestBase
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly MessagePublisher _messagePublisher;
        private readonly DataAvailableNotificationPublisherSpy _dataAvailableNotificationPublisherSpy;

        public MessagePublisherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _correlationContext = GetService<ICorrelationContext>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messagePublisher = GetService<MessagePublisher>();
            _dataAvailableNotificationPublisherSpy = (DataAvailableNotificationPublisherSpy)GetService<IDataAvailableNotificationPublisher>();
        }

        [Fact]
        public async Task Outgoing_messages_are_published()
        {
            var outgoingMessage = CreateOutgoingMessage();
            await StoreOutgoingMessage(outgoingMessage).ConfigureAwait(false);

            await _messagePublisher.PublishAsync().ConfigureAwait(false);

            var unpublishedMessages = _outgoingMessageStore.GetUnpublished();
            var publishedMessage = _dataAvailableNotificationPublisherSpy.GetMessageFrom(outgoingMessage.CorrelationId);
            Assert.Empty(unpublishedMessages);
            Assert.NotNull(publishedMessage);
        }

        private async Task StoreOutgoingMessage(OutgoingMessage outgoingMessage)
        {
            _outgoingMessageStore.Add(outgoingMessage);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = IncomingMessageBuilder.CreateMessage();
            return new OutgoingMessage("FakeDocumentType", transaction.Message.ReceiverId, _correlationContext.Id, transaction.MarketActivityRecord.Id, transaction.Message.ProcessType);
        }
    }
}
