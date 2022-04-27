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
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using B2B.Transactions.Configuration;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml;
using B2B.Transactions.Xml.Incoming;
using Xunit;

namespace B2B.Transactions.IntegrationTests.OutgoingMessages
{
    public class MessageRequestTests : TestBase
    {
        private readonly IOutgoingMessageStore _outgoingMessageStore;
        private readonly MessageRequestHandler _messageRequestHandler;
        private readonly IncomingMessageHandler _incomingMessageHandler;
        private readonly MessageDispatcher _messageDispatcher;

        public MessageRequestTests(DatabaseFixture databaseFixture)
            : base(databaseFixture)
        {
            _outgoingMessageStore = GetService<IOutgoingMessageStore>();
            _incomingMessageHandler = GetService<IncomingMessageHandler>();
            _messageRequestHandler = GetService<MessageRequestHandler>();
            _messageDispatcher = GetService<MessageDispatcher>();
        }

        [Fact]
        public async Task Messages_must_originate_from_the_same_type_of_business_process()
        {
            var builder = new IncomingMessageBuilder();
            var message1 = await MessageArrived(
                builder
                    .WithProcessType("ProcessType1")
                    .Build()).ConfigureAwait(false);
            var message2 = await MessageArrived(
                builder
                .WithProcessType("ProcessType2")
                .Build()).ConfigureAwait(false);
            var outgoingMessage1 = _outgoingMessageStore.GetByOriginalMessageId(message1.Id)!;
            var outgoingMessage2 = _outgoingMessageStore.GetByOriginalMessageId(message2.Id)!;

            var result = await _messageRequestHandler.HandleAsync(new List<string>()
            {
                outgoingMessage1.Id.ToString(),
                outgoingMessage2.Id.ToString(),
            }).ConfigureAwait(false);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Messages_must_same_receipient()
        {
            var builder = new IncomingMessageBuilder();
            var message1 = await MessageArrived(
                builder
                    .WithSenderId("SenderId1")
                    .Build()).ConfigureAwait(false);
            var message2 = await MessageArrived(
                builder
                    .WithSenderId("SenderId2")
                    .Build()).ConfigureAwait(false);
            var outgoingMessage1 = _outgoingMessageStore.GetByOriginalMessageId(message1.Id)!;
            var outgoingMessage2 = _outgoingMessageStore.GetByOriginalMessageId(message2.Id)!;

            var result = await _messageRequestHandler.HandleAsync(new List<string>()
            {
                outgoingMessage1.Id.ToString(),
                outgoingMessage2.Id.ToString(),
            }).ConfigureAwait(false);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Message_is_dispatched_on_request()
        {
            var incomingMessage1 = await MessageArrived().ConfigureAwait(false);
            var incomingMessage2 = await MessageArrived().ConfigureAwait(false);
            var outgoingMessage1 = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage1.Id)!;
            var outgoingMessage2 = _outgoingMessageStore.GetByOriginalMessageId(incomingMessage2.Id)!;

            var requestedMessageIds = new List<string> { outgoingMessage1.Id.ToString(), outgoingMessage2.Id.ToString(), };
            var result = await _messageRequestHandler.HandleAsync(requestedMessageIds.AsReadOnly()).ConfigureAwait(false);

            Assert.True(result.Success);

            var dispatchedMessage = _messageDispatcher.DispatchedMessage;
            var message = XDocument.Load(dispatchedMessage!);
            AssertXmlMessage.AssertMarketActivityRecordCount(message, 2);
            AssertMarketActivityRecord(message, incomingMessage1, outgoingMessage1);
            AssertMessageHeader(message, incomingMessage1);
            await AssertMessageConformsToSchema(dispatchedMessage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Requested_message_ids_must_exist()
        {
            var nonExistingMessage = new List<string> { Guid.NewGuid().ToString() };

            var result = await _messageRequestHandler.HandleAsync(nonExistingMessage.AsReadOnly()).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, error => error is OutgoingMessageNotFoundException);
        }

        private static void AssertMessageHeader(XDocument document, IncomingMessage incomingMessage)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", "E03");
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", DataHubDetails.IdentificationNumber);
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", incomingMessage.Message.SenderId);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", incomingMessage.Message.SenderRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", "A01");
        }

        private static void AssertMarketActivityRecord(XDocument document, IncomingMessage incomingMessage, OutgoingMessage outgoingMessage)
        {
            var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, outgoingMessage.Id.ToString())!;

            Assert.NotNull(marketActivityRecord);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", incomingMessage.MarketActivityRecord.Id);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "marketEvaluationPoint.mRID", incomingMessage.MarketActivityRecord.MarketEvaluationPointId);
        }

        private async Task AssertMessageConformsToSchema(Stream? dispatchedMessage)
        {
            var schema = await GetService<ISchemaProvider>().GetSchemaAsync("confirmrequestchangeofsupplier", "1.0")
                .ConfigureAwait(false);
            var validationResult = await MessageValidator.ValidateAsync(dispatchedMessage!, schema!).ConfigureAwait(false);
            Assert.True(validationResult.IsValid);
        }

        private async Task<IncomingMessage> MessageArrived()
        {
            var incomingMessage = IncomingMessageBuilder.CreateMessage();
            await _incomingMessageHandler.HandleAsync(incomingMessage).ConfigureAwait(false);
            return incomingMessage;
        }

        private async Task<IncomingMessage> MessageArrived(IncomingMessage arrivedMessage)
        {
            await _incomingMessageHandler.HandleAsync(arrivedMessage).ConfigureAwait(false);
            return arrivedMessage;
        }
    }
}
