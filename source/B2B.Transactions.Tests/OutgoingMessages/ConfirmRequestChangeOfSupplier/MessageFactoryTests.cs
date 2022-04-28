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
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using B2B.Transactions.Xml;
using B2B.Transactions.Xml.Incoming;
using Xunit;

namespace B2B.Transactions.Tests.OutgoingMessages.ConfirmRequestChangeOfSupplier
{
    public class MessageFactoryTests
    {
        private readonly MessageFactory _messageFactory;
        private readonly ISchemaProvider _schemaProvider;

        public MessageFactoryTests()
        {
            _schemaProvider = new SchemaProvider(new SchemaStore());
            _messageFactory = new MessageFactory(new SystemDateTimeProviderStub());
        }

        [Fact]
        public async Task Message_is_valid()
        {
            var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ");
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
            };

            var message = await _messageFactory.CreateFromAsync(header, marketActivityRecords).ConfigureAwait(false);

            await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
        }

        private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, XDocument document)
        {
            AssertXmlMessage.AssertMarketActivityRecordCount(document, 2);
            foreach (var activityRecord in marketActivityRecords)
            {
                var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, activityRecord.Id)!;

                Assert.NotNull(marketActivityRecord);
                AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", activityRecord.OriginalTransactionId);
                AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "marketEvaluationPoint.mRID", activityRecord.MarketEvaluationPointId);
            }
        }

        private static void AssertHeader(MessageHeader header, XDocument document)
        {
            Assert.NotEmpty(AssertXmlMessage.GetMessageHeaderValue(document, "mRID")!);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");
            AssertXmlMessage.AssertHasHeaderValue(document, "process.processType", header.ProcessType);
            AssertXmlMessage.AssertHasHeaderValue(document, "businessSector.type", "23");
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.mRID", header.SenderId);
            AssertXmlMessage.AssertHasHeaderValue(document, "sender_MarketParticipant.marketRole.type", "DDZ");
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.mRID", header.ReceiverId);
            AssertXmlMessage.AssertHasHeaderValue(document, "receiver_MarketParticipant.marketRole.type", header.ReceiverRole);
            AssertXmlMessage.AssertHasHeaderValue(document, "reason.code", "A01");
        }

        private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
        {
            var document = XDocument.Load(message);
            AssertHeader(header, document);

            AssertMarketActivityRecords(marketActivityRecords, document);

            await AssertConformsToSchema(message).ConfigureAwait(false);
        }

        private async Task AssertConformsToSchema(Stream message)
        {
            var schema = await _schemaProvider.GetSchemaAsync("confirmrequestchangeofsupplier", "1.0")
                .ConfigureAwait(false);
            var validationResult = await MessageValidator.ValidateAsync(message, schema!).ConfigureAwait(false);
            Assert.True(validationResult.IsValid);
        }
    }
}
