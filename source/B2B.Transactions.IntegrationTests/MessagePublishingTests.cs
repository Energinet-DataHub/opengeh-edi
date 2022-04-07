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
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly IMessageFactory<IMessage> _messageFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

        public MessagePublishingTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
            _outgoingMessageStore = new OutgoingMessageStoreSpy();
            _messageFactory = new AcceptMessageFactory(_systemDateTimeProvider);
        }

        [Fact]
        public async Task Outgoing_messages_are_published()
        {
            var dataAvailableNotificationSenderSpy = new DataAvailableNotificationSenderSpy();
            var messagePublisher = new MessagePublisher(dataAvailableNotificationSenderSpy);
            _outgoingMessageStore.Add(_messageFactory.CreateMessage(CreateTransaction()));

            await messagePublisher.PublishAsync(await _outgoingMessageStore.GetUnpublishedAsync().ConfigureAwait(false)).ConfigureAwait(false);
            var unpublishedMessages = await _outgoingMessageStore.GetUnpublishedAsync().ConfigureAwait(false);
            var publishedMessage = dataAvailableNotificationSenderSpy.PublishedMessages.FirstOrDefault();

            Assert.Empty(unpublishedMessages);
            Assert.NotNull(publishedMessage);
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

    #pragma warning disable
    public class MessagePublisher
    {
        private readonly IDataAvailableNotificationSender _dataAvailableNotificationSender;

        public MessagePublisher(IDataAvailableNotificationSender dataAvailableNotificationSender)
        {
            _dataAvailableNotificationSender = dataAvailableNotificationSender ?? throw new ArgumentNullException(nameof(dataAvailableNotificationSender));
        }


        public async Task PublishAsync(ReadOnlyCollection<IMessage> unpublishedMessages)
        {
            foreach (var message in unpublishedMessages)
            {
                await _dataAvailableNotificationSender.SendAsync(
                    "CorrelationId",
                    new DataAvailableNotificationDto(
                        Guid.NewGuid(),
                        new GlobalLocationNumberDto("RecipientId"),
                        new MessageTypeDto("MessageType"),
                        DomainOrigin.MarketRoles,
                        true,
                        1,
                        "DocumentType")).ConfigureAwait(false);

                message.Published();
            }
        }
    }

    public class DataAvailableNotificationSenderSpy : IDataAvailableNotificationSender
    {
        public ReadOnlyCollection<DataAvailableNotificationDto> PublishedMessages => _publishedMessages.AsReadOnly();

        private List<DataAvailableNotificationDto> _publishedMessages = new();
        public Task SendAsync(string correlationId, DataAvailableNotificationDto dataAvailableNotificationDto)
        {
            _publishedMessages.Add(dataAvailableNotificationDto);
            return Task.CompletedTask;
        }
    }
}
