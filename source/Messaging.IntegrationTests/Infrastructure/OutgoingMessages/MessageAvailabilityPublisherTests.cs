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
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace Messaging.IntegrationTests.Infrastructure.OutgoingMessages
{
    [IntegrationTest]
    public class MessageAvailabilityPublisherTests : TestBase
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly MessageAvailabilityPublisher _messageAvailabilityPublisher;
        private readonly NewMessageAvailableNotifierSpy _newMessageAvailableNotifierSpy;

        public MessageAvailabilityPublisherTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _correlationContext = GetService<ICorrelationContext>();
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _messageAvailabilityPublisher = GetService<MessageAvailabilityPublisher>();
            _newMessageAvailableNotifierSpy = (NewMessageAvailableNotifierSpy)GetService<INewMessageAvailableNotifier>();
        }

        [Fact]
        public async Task Outgoing_messages_are_published()
        {
            var outgoingMessage = CreateOutgoingMessage();
            await StoreOutgoingMessage(outgoingMessage).ConfigureAwait(false);

            await _messageAvailabilityPublisher.PublishAsync().ConfigureAwait(false);

            var unpublishedMessages = _outgoingMessageStore.GetUnpublished();
            var publishedMessage = _newMessageAvailableNotifierSpy.GetMessageFrom(outgoingMessage.Id);
            Assert.Empty(unpublishedMessages);
            Assert.NotNull(publishedMessage);
        }

        private static OutgoingMessage CreateOutgoingMessage()
        {
            var transaction = new IncomingMessageBuilder().Build();
            return new OutgoingMessage(
                DocumentType.GenericNotification,
                transaction.Message.ReceiverId,
                transaction.MarketActivityRecord.Id,
                transaction.Message.ProcessType,
                transaction.Message.ReceiverRole,
                transaction.Message.SenderId,
                transaction.Message.SenderRole,
                string.Empty);
        }

        private async Task StoreOutgoingMessage(OutgoingMessage outgoingMessage)
        {
            _outgoingMessageStore.Add(outgoingMessage);
            await GetService<IUnitOfWork>().CommitAsync().ConfigureAwait(false);
        }
    }
}
