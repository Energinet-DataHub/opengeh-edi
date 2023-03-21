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
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;
using Application.Configuration;
using Application.OutgoingMessages.Common;
using DocumentValidation;
using DocumentValidation.CimXml;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.GenericNotification;
using Infrastructure.Configuration;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.GenericNotification;
using Tests.Factories;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Tests.Infrastructure.OutgoingMessages.GenericNotification
{
    public class GenericNotificationDocumentWriterTests
    {
        private readonly GenericNotificationMessageWriter _messageWriter;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMessageRecordParser _messageRecordParser;
        private ISchemaProvider? _schemaProvider;

        public GenericNotificationDocumentWriterTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProvider();
            _messageRecordParser = new MessageRecordParser(new Serializer());
            _messageWriter = new GenericNotificationMessageWriter(_messageRecordParser);
        }

        [Fact]
        public async Task Document_is_valid()
        {
            var header = MessageHeaderFactory.Create();
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId", _systemDateTimeProvider.Now()),
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId", _systemDateTimeProvider.Now()),
            };

            var message = await _messageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList()).ConfigureAwait(false);

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
                AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "validityStart_DateAndOrTime.dateTime", activityRecord.ValidityStart.ToString());
            }
        }

        private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
        {
            var document = XDocument.Load(message);
            AssertXmlMessage.AssertHeader(header, document);
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "E44");

            AssertMarketActivityRecords(marketActivityRecords, document);

            await AssertConformsToSchema(message).ConfigureAwait(false);
        }

        private async Task AssertConformsToSchema(Stream message)
        {
            _schemaProvider = new CimXmlSchemaProvider();
            var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("genericnotification", "0.1")
                .ConfigureAwait(false);
            await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
        }
    }
}
