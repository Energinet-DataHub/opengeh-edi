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

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.OutgoingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Model.Model;
using Xunit;
using Xunit.Categories;

namespace B2B.Transactions.IntegrationTests.Infrastructure.OutgoingMessages
{
    [IntegrationTest]
    public class MessagePublisherTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageFactory<IDocument> _messageFactory;
        private readonly MessagePublisher _messagePublisher;
        private readonly DataAvailableNotificationSenderSpy _dataAvailableNotificationSenderSpy;
        private readonly OutgoingMessageParser _outgoingMessageParser;

        public MessagePublisherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            var systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageFactory = new AcceptMessageFactory(systemDateTimeProvider);
            _messagePublisher = GetService<MessagePublisher>();
            _dataAvailableNotificationSenderSpy = (DataAvailableNotificationSenderSpy)GetService<IDataAvailableNotificationSender>();
            _outgoingMessageParser = new OutgoingMessageParser(new SchemaProvider(new SchemaStore()));
        }

        [Fact]
        public async Task Outgoing_messages_are_published()
        {
            var outgoingMessage = CreateOutgoingMessage();

            await _messagePublisher.PublishAsync(_outgoingMessageStore.GetUnpublished()).ConfigureAwait(false);

            var unpublishedMessages = _outgoingMessageStore.GetUnpublished();
            var publishedMessage = _dataAvailableNotificationSenderSpy.PublishedMessages.FirstOrDefault();

            Assert.Empty(unpublishedMessages);
            Assert.NotNull(publishedMessage);
            Assert.Equal(outgoingMessage.RecipientId, publishedMessage?.Recipient.Value);
            Assert.Equal(DomainOrigin.MarketRoles, publishedMessage?.Origin);
            Assert.Equal(outgoingMessage.DocumentType, publishedMessage?.DocumentType);
            Assert.Equal(false, publishedMessage?.SupportsBundling);
            Assert.Equal(string.Empty, publishedMessage?.MessageType.Value);
        }

        [Fact]
        public async Task Outgoing_messages_must_conform_to_schema()
        {
            var outgoingMessage = CreateOutgoingMessage();

            var stream = CreateStreamFromString(outgoingMessage.MessagePayload);
            var validationResult = await _outgoingMessageParser.ParseAsync(stream, "confirmrequestchangeofsupplier", "1.0").ConfigureAwait(false);
            await stream.DisposeAsync().ConfigureAwait(false);

            Assert.Empty(validationResult);
        }

        private static Stream CreateStreamFromString(string input)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(input));
        }

        private OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = TransactionBuilder.CreateTransaction();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage =
                new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId);
            _outgoingMessageStore.Add(outgoingMessage);
            return outgoingMessage;
        }
    }
}
