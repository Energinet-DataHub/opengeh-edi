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
using System.Linq;
using System.Xml.Linq;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Acknowledgements
{
    public class AcknowledgementXmlSerializer
    {
        public string Serialize(ConfirmMessage message, XNamespace xmlNamespace)
        {
            var document = new XDocument(
                new XElement(
                    xmlNamespace + message.DocumentName,
                    new XAttribute(XNamespace.Xmlns + "cim", xmlNamespace),
                    new XElement(xmlNamespace + "mRID", Guid.NewGuid().ToString()),
                    new XElement(xmlNamespace + "type", message.Type),
                    new XElement(xmlNamespace + "process.processType", message.ProcessType),
                    new XElement(xmlNamespace + "businessSector.type", message.BusinessSectorType),
                    new XElement(xmlNamespace + "sender_MarketParticipant.mRID", new XAttribute("codingScheme", message.Sender.CodingScheme), message.Sender.Id),
                    new XElement(xmlNamespace + "sender_MarketParticipant.marketRole.type", message.Sender.Role),
                    new XElement(xmlNamespace + "receiver_MarketParticipant.mRID", new XAttribute("codingScheme", message.Receiver.CodingScheme), message.Receiver.Id),
                    new XElement(xmlNamespace + "receiver_MarketParticipant.marketRole.type", message.Receiver.Role),
                    new XElement(xmlNamespace + "createdDateTime", message.CreatedDateTime),
                    new XElement(xmlNamespace + "reason.code", message.ReasonCode),
                    new XElement(
                        xmlNamespace + "MktActivityRecord",
                        new XElement(xmlNamespace + "mRID", message.MarketActivityRecord.Id),
                        new XElement(xmlNamespace + "businessProcessReference_MktActivityRecord.mRID", message.MarketActivityRecord.BusinessProcessReference),
                        new XElement(xmlNamespace + "originalTransactionReference_MktActivityRecord.mRID", message.MarketActivityRecord.OriginalTransaction),
                        new XElement(xmlNamespace + "marketEvaluationPoint.mRID", message.MarketActivityRecord.MarketEvaluationPoint),
                        new XElement(xmlNamespace + "start_DateAndOrTime.date", message.MarketActivityRecord.StartDateAndOrTime))));

            return Serialize(document);
        }

        public string Serialize(RejectMessage message, XNamespace xmlNamespace)
        {
            var document = new XDocument(
                new XElement(
                    xmlNamespace + message.DocumentName,
                    new XAttribute(XNamespace.Xmlns + "cim", xmlNamespace),
                    new XElement(xmlNamespace + "mRID", message.Id),
                    new XElement(xmlNamespace + "type", message.Type),
                    new XElement(xmlNamespace + "process.processType", message.ProcessType),
                    new XElement(xmlNamespace + "businessSector.type", message.BusinessSectorType),
                    new XElement(xmlNamespace + "sender_MarketParticipant.mRID", new XAttribute("codingScheme", message.Sender.CodingScheme), message.Sender.Id),
                    new XElement(xmlNamespace + "sender_MarketParticipant.marketRole.type", message.Sender.Role),
                    new XElement(xmlNamespace + "receiver_MarketParticipant.mRID", new XAttribute("codingScheme", message.Receiver.CodingScheme), message.Receiver.Id),
                    new XElement(xmlNamespace + "receiver_MarketParticipant.marketRole.type", message.Receiver.Role),
                    new XElement(xmlNamespace + "createdDateTime", message.CreatedDateTime),
                    GetReasonElement(xmlNamespace, message.Reason.Code, message.Reason.Text),
                    new XElement(
                        xmlNamespace + "MktActivityRecord",
                        new XElement(xmlNamespace + "mRID", message.MarketActivityRecord.Id),
                        new XElement(xmlNamespace + "businessProcessReference_MktActivityRecord.mRID", message.MarketActivityRecord.BusinessProcessReference),
                        new XElement(xmlNamespace + "originalTransactionReference_MktActivityRecord.mRID", message.MarketActivityRecord.OriginalTransaction),
                        new XElement(xmlNamespace + "marketEvaluationPoint.mRID", message.MarketActivityRecord.MarketEvaluationPoint),
                        new XElement(xmlNamespace + "start_DateAndOrTime.date", message.MarketActivityRecord.StartDateAndOrTime),
                        message.MarketActivityRecord.Reasons.Select(r => GetReasonElement(xmlNamespace, r.Code, r.Text)))));

            return Serialize(document);
        }

        private static string Serialize(XDocument document)
        {
            using (var writer = new Utf8StringWriter())
            {
                document.Save(writer);
                return writer.ToString();
            }
        }

        private static XElement GetReasonElement(XNamespace xmlNamespace, string code, string text)
        {
            return new XElement(
                xmlNamespace + "Reason",
                new XElement(xmlNamespace + "code", code),
                new XElement(xmlNamespace + "text", text));
        }
    }
}
