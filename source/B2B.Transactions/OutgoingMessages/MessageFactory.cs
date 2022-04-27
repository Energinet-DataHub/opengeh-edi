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
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace B2B.Transactions.OutgoingMessages
{
    public class MessageFactory
    {
        private readonly IncomingMessageStore _incomingMessageStore;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly MessageValidator _messageValidator;

        public MessageFactory(IncomingMessageStore incomingMessageStore, ISystemDateTimeProvider systemDateTimeProvider, MessageValidator messageValidator)
        {
            _incomingMessageStore = incomingMessageStore;
            _systemDateTimeProvider = systemDateTimeProvider;
            _messageValidator = messageValidator;
        }

        public async Task<Stream> CreateFromAsync(ReadOnlyCollection<OutgoingMessage> messages)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            const string Prefix = "cim";

            var incomingMessage = _incomingMessageStore.GetById(messages[0].OriginalMessageId);

            if (incomingMessage is null)
            {
                throw new InvalidOperationException();
            }

            var settings = new XmlWriterSettings { OmitXmlDeclaration = false, Encoding = Encoding.UTF8, Async = true };
            using var stream = new MemoryStream();
            using var output = new Utf8StringWriter();
            using var writer = XmlWriter.Create(output, settings);

            await writer.WriteStartDocumentAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(Prefix, "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(
                "xsi",
                "schemaLocation",
                null,
                "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd").ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "mRID", null, GenerateMessageId()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "type", null, "414").ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "process.processType", null, incomingMessage.Message.ProcessType).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "businessSector.type", null, "23").ConfigureAwait(false);

            await writer.WriteStartElementAsync(Prefix, "sender_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue("5790001330552");
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(Prefix, "sender_MarketParticipant.marketRole.type", null, "DDZ").ConfigureAwait(false);

            await writer.WriteStartElementAsync(Prefix, "receiver_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(incomingMessage.Message.SenderId);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(Prefix, "receiver_MarketParticipant.marketRole.type", null, incomingMessage.Message.SenderRole).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "createdDateTime", null, GetCurrentDateTime()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "reason.code", null, "A01").ConfigureAwait(false);

            foreach (var message in messages)
            {
                var originalMessage = _incomingMessageStore.GetById(message.OriginalMessageId);

                if (originalMessage is null)
                {
                    throw new InvalidOperationException();
                }

                await writer.WriteStartElementAsync(Prefix, "MktActivityRecord", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Prefix, "mRID", null, message.Id.ToString()).ConfigureAwait(false);
                await writer.WriteElementStringAsync(Prefix, "originalTransactionIDReference_MktActivityRecord.mRID", null, originalMessage.MarketActivityRecord.Id).ConfigureAwait(false);
                await writer.WriteStartElementAsync(Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
                writer.WriteValue(originalMessage.MarketActivityRecord.MarketEvaluationPointId);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            writer.Close();
            await output.FlushAsync().ConfigureAwait(false);

            await ValidateXmlAgainstSchemaAsync(output).ConfigureAwait(false);

            var data = Encoding.UTF8.GetBytes(output.ToString());

            return new MemoryStream(data);
        }

        private static string GenerateMessageId()
        {
            return MessageIdGenerator.Generate();
        }

        private string GetCurrentDateTime()
        {
            return _systemDateTimeProvider.Now().ToString();
        }

        private async Task ValidateXmlAgainstSchemaAsync(Utf8StringWriter output)
        {
            await _messageValidator.ParseAsync(output.ToString(), "confirmrequestchangeofsupplier", "1.0").ConfigureAwait(false);
            if (!_messageValidator.Success)
            {
                throw new InvalidOperationException(
                    $"Generated accept message does not conform with XSD schema definition: {_messageValidator.Errors()}");
            }
        }
    }
}
