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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Schema;
using B2B.CimMessageAdapter.Tests.Stubs;
using Xunit;

namespace B2B.CimMessageAdapter.Tests
{
    public class MessageReceiverTests
    {
        private readonly ActorContextStub _actorContextStub = new();
        private readonly TransactionIdsStub _transactionIdsStub = new();
        private readonly MessageIdsStub _messageIdsStub = new();
        private TransactionQueueDispatcherStub _transactionQueueDispatcherSpy = new();

        [Fact]
        public async Task Sender_id_must_match_the_organization_of_the_current_authenticated_user()
        {
            _actorContextStub.UseInvalidActor();
            await using var message = CreateMessage();
            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-008");
        }

        [Fact]
        public async Task Message_must_be_valid_xml()
        {
            await using var message = CreateMessageWithInvalidXmlStructure();
            {
                var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

                Assert.False(result.Success);
                AssertContainsError(result, "B2B-005");
            }
        }

        [Fact]
        public async Task Message_must_conform_to_xml_schema()
        {
            await using var message = CreateMessageNotConformingToXmlSchema();
            var result = await ReceiveRequestChangeOfSupplierMessage(message).ConfigureAwait(false);

            AssertContainsError(result, "B2B-005");
        }

        [Fact]
        public async Task Return_failure_if_xml_schema_for_business_process_type_does_not_exist()
        {
            await using var message = CreateMessage();

            var result = await ReceiveRequestChangeOfSupplierMessage(message, "non_existing_version")
                .ConfigureAwait(false);

            Assert.False(result.Success);
            AssertContainsError(result, "B2B-001");
        }

        [Fact]
        public async Task Valid_activity_records_are_extracted_and_committed_to_queue()
        {
            await using var message = CreateMessage();
            await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            var transaction = _transactionQueueDispatcherSpy.CommittedItems.FirstOrDefault();
            Assert.NotNull(transaction);
            Assert.Equal("78954612", transaction?.Message.MessageId);
            Assert.Equal("E65", transaction?.Message.ProcessType);
            Assert.Equal("5799999933318", transaction?.Message.SenderId);
            Assert.Equal("DDQ", transaction?.Message.SenderRole);
            Assert.Equal("5790001330552", transaction?.Message.ReceiverId);
            Assert.Equal("DDZ", transaction?.Message.ReceiverRole);
            Assert.Equal("2022-09-07T09:30:47Z", transaction?.Message.CreatedAt);
            Assert.Equal("12345689", transaction?.MarketActivityRecord.Id);
            Assert.Equal("579999993331812345", transaction?.MarketActivityRecord.MarketEvaluationPointId);
            Assert.Equal("5799999933318", transaction?.MarketActivityRecord.EnergySupplierId);
            Assert.Equal("5799999933340", transaction?.MarketActivityRecord.BalanceResponsibleId);
            Assert.Equal("0801741527", transaction?.MarketActivityRecord.ConsumerId);
            Assert.Equal("Jan Hansen", transaction?.MarketActivityRecord.ConsumerName);
            Assert.Equal("2022-09-07T22:00:00Z", transaction?.MarketActivityRecord.EffectiveDate);
        }

        [Fact]
        public async Task Activity_records_are_not_committed_to_queue_if_any_message_header_values_are_invalid()
        {
            var messageIds = new MessageIdsStub();
            await SimulateDuplicationOfMessageIds(messageIds).ConfigureAwait(false);

            Assert.Empty(_transactionQueueDispatcherSpy.CommittedItems);
        }

        [Fact]
        public async Task Activity_records_must_have_unique_transaction_ids()
        {
            await using var message = CreateMessageWithDuplicateTransactionIds();
            var result = await ReceiveRequestChangeOfSupplierMessage(message)
                .ConfigureAwait(false);

            AssertContainsError(result, "B2B-005");
            Assert.Empty(_transactionQueueDispatcherSpy.CommittedItems);
        }

        private static Stream CreateMessageWithInvalidXmlStructure()
        {
            var messageStream = new MemoryStream();
            using var writer = new StreamWriter(messageStream);
            writer.Write("This is not XML");
            writer.Flush();
            messageStream.Position = 0;
            return messageStream;
        }

        private static Stream CreateMessageNotConformingToXmlSchema()
        {
            return CreateMessageFrom("Messages\\InvalidRequestChangeOfSupplier.xml");
        }

        private static Stream CreateMessage()
        {
            return CreateMessageFrom("Messages\\ValidRequestChangeOfSupplier.xml");
        }

        private static Stream CreateMessageWithDuplicateTransactionIds()
        {
            return CreateMessageFrom("Messages\\RequestChangeOfSupplierWithDuplicateTransactionIds.xml");
        }

        private static Stream CreateMessageFrom(string xmlFile)
        {
            using var fileReader = new FileStream(xmlFile, FileMode.Open, FileAccess.Read);
            var messageStream = new MemoryStream();
            fileReader.CopyTo(messageStream);
            messageStream.Position = 0;
            return messageStream;
        }

        private static void AssertContainsError(Result result, string errorCode)
        {
            Assert.Contains(result.Errors, error => error.Code.Equals(errorCode, StringComparison.OrdinalIgnoreCase));
        }

        private Task<Result> ReceiveRequestChangeOfSupplierMessage(Stream message, string version = "1.0")
        {
            return CreateMessageReceiver().ReceiveAsync(message, "requestchangeofsupplier", version);
        }

        private MessageReceiver CreateMessageReceiver()
        {
            _transactionQueueDispatcherSpy = new TransactionQueueDispatcherStub();
            var messageReceiver = new MessageReceiver(_messageIdsStub, _transactionQueueDispatcherSpy, _transactionIdsStub, new SchemaProvider(new SchemaStore()), _actorContextStub);
            return messageReceiver;
        }

        private MessageReceiver CreateMessageReceiver(IMessageIds messageIds)
        {
            _transactionQueueDispatcherSpy = new TransactionQueueDispatcherStub();
            var messageReceiver = new MessageReceiver(messageIds, _transactionQueueDispatcherSpy, _transactionIdsStub, new SchemaProvider(new SchemaStore()), _actorContextStub);
            return messageReceiver;
        }

        private async Task SimulateDuplicationOfMessageIds(IMessageIds messageIds)
        {
            await using (var message = CreateMessage())
            {
                await CreateMessageReceiver(messageIds).ReceiveAsync(message, "requestchangeofsupplier", "1.0")
                    .ConfigureAwait(false);
            }

            await using (var message = CreateMessage())
            {
                await CreateMessageReceiver(messageIds).ReceiveAsync(message, "requestchangeofsupplier", "1.0")
                    .ConfigureAwait(false);
            }
        }
    }
}
