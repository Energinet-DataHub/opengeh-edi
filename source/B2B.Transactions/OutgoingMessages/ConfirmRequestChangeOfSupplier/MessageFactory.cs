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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using B2B.Transactions.Common;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Newtonsoft.Json;

namespace B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier
{
    public class MessageFactory
    {
        private const string Prefix = "cim";
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IMarketActivityRecordParser _marketActivityRecordParser;

        public MessageFactory(ISystemDateTimeProvider systemDateTimeProvider, IMarketActivityRecordParser marketActivityRecordParser)
        {
            _systemDateTimeProvider = systemDateTimeProvider;
            _marketActivityRecordParser = marketActivityRecordParser;
        }

        public async Task<Stream> CreateFromAsync(MessageHeader messageHeader, IReadOnlyCollection<MarketActivityRecord> marketActivityRecords)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8, Async = true };
            var stream = new MemoryStream();
            await using var writer = XmlWriter.Create(stream, settings);
            await WriteMessageHeaderAsync(messageHeader, writer).ConfigureAwait(false);
            await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            writer.Close();
            stream.Position = 0;

            return stream;
        }

        public async Task<Stream> CreateFromAsync(MessageHeader messageHeader, IReadOnlyCollection<string> marketActivityPayloads)
        {
            if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
            if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8, Async = true };
            var stream = new MemoryStream();
            await using var writer = XmlWriter.Create(stream, settings);
            await WriteMessageHeaderAsync(messageHeader, writer).ConfigureAwait(false);
            var marketActivityRecords = new List<MarketActivityRecord>();
            foreach (var payload in marketActivityPayloads)
            {
                var marketActivityRecord =
                    _marketActivityRecordParser.From<MarketActivityRecord>(payload);
                marketActivityRecords.Add(marketActivityRecord);
            }

            await WriteMarketActivityRecordsAsync(marketActivityRecords, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            writer.Close();
            stream.Position = 0;

            return stream;
        }

        private static async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<MarketActivityRecord> marketActivityRecords, XmlWriter writer)
        {
            foreach (var marketActivityRecord in marketActivityRecords)
            {
                await writer.WriteStartElementAsync(Prefix, "MktActivityRecord", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Prefix, "mRID", null, marketActivityRecord.Id.ToString())
                    .ConfigureAwait(false);
                await writer.WriteElementStringAsync(
                    Prefix,
                    "originalTransactionIDReference_MktActivityRecord.mRID",
                    null,
                    marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
                await writer.WriteStartElementAsync(Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
                writer.WriteValue(marketActivityRecord.MarketEvaluationPointId);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }
        }

        private static string GenerateMessageId()
        {
            return MessageIdGenerator.Generate();
        }

        private async Task WriteMessageHeaderAsync(MessageHeader messageHeader, XmlWriter writer)
        {
            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(
                Prefix,
                "ConfirmRequestChangeOfSupplier_MarketDocument",
                "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance")
                .ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(
                    "xsi",
                    "schemaLocation",
                    null,
                    "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd")
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "mRID", null, GenerateMessageId()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "type", null, "414").ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "process.processType", null, messageHeader.ProcessType)
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "businessSector.type", null, "23").ConfigureAwait(false);

            await writer.WriteStartElementAsync(Prefix, "sender_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(messageHeader.SenderId);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(Prefix, "sender_MarketParticipant.marketRole.type", null, "DDZ")
                .ConfigureAwait(false);

            await writer.WriteStartElementAsync(Prefix, "receiver_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(messageHeader.ReceiverId);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer
                .WriteElementStringAsync(Prefix, "receiver_MarketParticipant.marketRole.type", null, messageHeader.ReceiverRole)
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "createdDateTime", null, GetCurrentDateTime()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "reason.code", null, "A01").ConfigureAwait(false);
        }

        private string GetCurrentDateTime()
        {
            return _systemDateTimeProvider.Now().ToString();
        }
    }
}
