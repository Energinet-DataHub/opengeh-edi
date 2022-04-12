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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.OutgoingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using B2B.Transactions.Messages;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Model.Model;
using Xunit;

namespace B2B.Transactions.IntegrationTests
{
    public class MessagePublishingTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStoreSpy;
        private readonly IMessageFactory<IDocument> _messageFactory;

        public MessagePublishingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            var systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
            _outgoingMessageStoreSpy = new OutgoingMessageStoreSpy();
            _messageFactory = new AcceptMessageFactory(systemDateTimeProvider);
        }

        [Fact]
        public async Task Outgoing_messages_are_published()
        {
            var dataAvailableNotificationSenderSpy = new DataAvailableNotificationPublisherSpy();
            var messagePublisher = new MessagePublisher(dataAvailableNotificationSenderSpy, GetService<ICorrelationContext>());
            var transaction = CreateTransaction();
            var document = _messageFactory.CreateMessage(transaction);
            var outgoingMessage = new OutgoingMessage(document.DocumentType, document.MessagePayload, transaction.Message.ReceiverId);
            _outgoingMessageStoreSpy.Add(outgoingMessage);

            await messagePublisher.PublishAsync(_outgoingMessageStoreSpy.GetUnpublished()).ConfigureAwait(false);
            var unpublishedMessages = _outgoingMessageStoreSpy.GetUnpublished();
            var publishedMessage = dataAvailableNotificationSenderSpy.PublishedMessages.FirstOrDefault();

            Assert.Empty(unpublishedMessages);
            Assert.NotNull(publishedMessage);
            Assert.Equal(outgoingMessage.RecipientId, publishedMessage?.Recipient.Value);
            Assert.Equal(DomainOrigin.MarketRoles, publishedMessage?.Origin);
            Assert.Equal(outgoingMessage.DocumentType, publishedMessage?.DocumentType);
            Assert.Equal(false, publishedMessage?.SupportsBundling);
            Assert.Equal(string.Empty, publishedMessage?.MessageType.Value);
        }

        private static B2BTransaction CreateTransaction()
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
    }
}
