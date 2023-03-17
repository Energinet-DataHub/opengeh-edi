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
using Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Infrastructure.Configuration;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Tests.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier
{
    public class ConfirmRequestChangeOfSupplierDocumentWriterTests
    {
        private readonly ConfirmChangeOfSupplierXmlMessageWriter _xmlMessageWriter;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMessageRecordParser _messageRecordParser;
        private ISchemaProvider? _schemaProvider;

        public ConfirmRequestChangeOfSupplierDocumentWriterTests()
        {
            _systemDateTimeProvider = new SystemDateTimeProvider();
            _messageRecordParser = new MessageRecordParser(new Serializer());
            _xmlMessageWriter = new ConfirmChangeOfSupplierXmlMessageWriter(_messageRecordParser);
        }

        [Fact]
        public async Task Document_is_valid()
        {
            var header = new MessageHeader(ProcessType.MoveIn.Name, "SenderId", MarketRole.MeteringPointAdministrator.Name, "ReceiverId", MarketRole.EnergySupplier.Name, Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
            var marketActivityRecords = new List<MarketActivityRecord>()
            {
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
                new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
            };

            var message = await _xmlMessageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList()).ConfigureAwait(false);

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

        private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
        {
            var document = XDocument.Load(message);
            AssertXmlMessage.AssertHeader(header, document);
            AssertXmlMessage.HasReasonCode(document, "A01");
            AssertXmlMessage.AssertHasHeaderValue(document, "type", "414");

            AssertMarketActivityRecords(marketActivityRecords, document);

            await AssertConformsToSchema(message).ConfigureAwait(false);
        }

        private async Task AssertConformsToSchema(Stream message)
        {
            _schemaProvider = new CimXmlSchemaProvider();
            var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("confirmrequestchangeofsupplier", "0.1")
                .ConfigureAwait(false);
            await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
        }
    }
}
