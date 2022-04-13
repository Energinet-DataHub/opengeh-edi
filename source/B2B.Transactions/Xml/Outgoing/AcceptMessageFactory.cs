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

using System.Text;
using System.Xml;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;

namespace B2B.Transactions.Xml.Outgoing
{
    public class AcceptMessageFactory : MessageFactory<IDocument>
    {
        private const string MessageType = "ConfirmRequestChangeOfSupplier";
        private const string Prefix = "cim";

        public AcceptMessageFactory(ISystemDateTimeProvider systemDateTimeProvider)
            : base(systemDateTimeProvider)
        {
        }

        public override IDocument CreateMessage(B2BTransaction transaction)
        {
            var settings = new XmlWriterSettings() { OmitXmlDeclaration = false, Encoding = Encoding.UTF8 };
            using var output = new Utf8StringWriter();
            using var writer = XmlWriter.Create(output, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement(Prefix, "ConfirmRequestChangeOfSupplier_MarketDocument", "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xsi", "schemaLocation", null, "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd");
            writer.WriteElementString(Prefix, "mRID", null, GenerateMessageId());
            writer.WriteElementString(Prefix, "type", null, "414");
            writer.WriteElementString(Prefix, "process.processType", null, transaction?.Message.ProcessType);
            writer.WriteElementString(Prefix, "businessSector.type", null, "23");

            writer.WriteStartElement(Prefix, "sender_MarketParticipant.mRID", null);
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue("5790001330552");
            writer.WriteEndElement();

            writer.WriteElementString(Prefix, "sender_MarketParticipant.marketRole.type", null, "DDZ");

            writer.WriteStartElement(Prefix, "receiver_MarketParticipant.mRID", null);
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction?.Message.SenderId);
            writer.WriteEndElement();

            writer.WriteElementString(Prefix, "receiver_MarketParticipant.marketRole.type", null, transaction?.Message.SenderRole);
            writer.WriteElementString(Prefix, "createdDateTime", null, GetCurrentDateTime());
            writer.WriteElementString(Prefix, "reason.code", null, "A01");

            writer.WriteStartElement(Prefix, "MktActivityRecord", null);
            writer.WriteElementString(Prefix, "mRID", null, GenerateTransactionId());
            writer.WriteElementString(Prefix, "originalTransactionIDReference_MktActivityRecord.mRID", null, transaction?.MarketActivityRecord.Id);

            writer.WriteStartElement(Prefix, "marketEvaluationPoint.mRID", null);
            writer.WriteAttributeString(null, "codingScheme", null, "A10");
            writer.WriteValue(transaction?.MarketActivityRecord.EnergySupplierId);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Close();
            output.Flush();

            return new AcceptMessage(output.ToString(), MessageType);
        }
    }
}
