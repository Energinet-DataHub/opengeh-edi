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
using System.Xml.Linq;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.CimMessageAdapter.Messages
{
    public class BusinessMessageBuilder
    {
        private readonly XNamespace _xmlNamespace;
        private readonly XDocument _document;

        private BusinessMessageBuilder(string pathToXmlFile, string xmlNamespace)
        {
            _xmlNamespace = xmlNamespace;
            _document = XDocument.Load(pathToXmlFile);
        }

        public static BusinessMessageBuilder RequestAggregatedMeasureData(string pathToXmlFile = "Infrastructure.CimMessageAdapter//Messages//Xml//RequestAggregatedMeasureData.xml")
        {
            return new BusinessMessageBuilder(pathToXmlFile, "urn:ediel.org:measure:requestaggregatedmeasuredata:0:1");
        }

        public static BusinessMessageBuilder RequestWholesaleServices(string pathToXmlFile = "Infrastructure.CimMessageAdapter//Messages//Xml//RequestWholesaleSettlement.xml")
        {
            return new BusinessMessageBuilder(pathToXmlFile, "urn:ediel.org:measure:requestwholesalesettlement:0:1");
        }

        public BusinessMessageBuilder WithSenderRole(string roleType)
        {
            SetRootChildElementValue("sender_MarketParticipant.marketRole.type", roleType);
            return this;
        }

        public BusinessMessageBuilder WithProcessType(string processType)
        {
            SetRootChildElementValue("process.processType", processType);
            return this;
        }

        public BusinessMessageBuilder WithMessageType(string messageType)
        {
            SetRootChildElementValue("type", messageType);
            return this;
        }

        public BusinessMessageBuilder WithReceiverRole(string roleType)
        {
            SetRootChildElementValue("receiver_MarketParticipant.marketRole.type", roleType);
            return this;
        }

        public BusinessMessageBuilder WithReceiverId(string receiverId)
        {
            SetRootChildElementValue("receiver_MarketParticipant.mRID", receiverId);
            return this;
        }

        public BusinessMessageBuilder WithMessageId(string messageId)
        {
            SetRootChildElementValue("mRID", messageId);
            return this;
        }

        public BusinessMessageBuilder WithSenderId(string senderId)
        {
            SetRootChildElementValue("sender_MarketParticipant.mRID", senderId);
            return this;
        }

        public BusinessMessageBuilder WithBusinessType(string businessType)
        {
            SetRootChildElementValue("businessSector.type", businessType);
            return this;
        }

        public BusinessMessageBuilder WithSeriesTransactionId(string transactionId)
        {
            var root = _document.Root;
            var serieElement = root!
                .Element(_xmlNamespace + "Series")!
                .Elements()
                .First(serieElement => serieElement.Name.LocalName!.Equals("mRID", StringComparison.Ordinal));

            serieElement!.Value = transactionId;
            return this;
        }

        public Stream Message()
        {
            var message = new MemoryStream();
            _document.Save(message, SaveOptions.DisableFormatting);
            message.Position = 0;
            return message;
        }

        public BusinessMessageBuilder DuplicateMarketActivityRecords()
        {
            var root = _document.Root;
            var marketActivityRecord = root!
                .Element(_xmlNamespace + "MktActivityRecord");

            root.Add(marketActivityRecord);
            return this;
        }

        public BusinessMessageBuilder DuplicateSeriesRecords()
        {
            // TODO: Consider merging DuplicateMarketActivityRecords and DuplicateSeriesRecords
            var root = _document.Root;
            var seriesElement = root!
                .Element(_xmlNamespace + "Series");

            root.Add(seriesElement);
            return this;
        }

        public BusinessMessageBuilder WithEnergySupplierId(string energySupplierId)
        {
            SetElementFromMarketActivityRecordValue("marketEvaluationPoint.energySupplier_MarketParticipant.mRID", energySupplierId);
            return this;
        }

        public BusinessMessageBuilder RemoveElementFromMarketActivityRecord(string elementName)
        {
            var elementToRemove = _document.Root?.Element(_xmlNamespace + "MktActivityRecord")?.Element(_xmlNamespace + elementName);
            elementToRemove?.Remove();
            return this;
        }

        public BusinessMessageBuilder RemoveElementFromMarketActivityRecordValue(string elementName)
        {
            GetMarketActivityRecordChildElement(elementName).Value = string.Empty;
            return this;
        }

        private void SetRootChildElementValue(string elementName, string value)
        {
            _document.Root!
                .Element(_xmlNamespace + elementName)!.Value = value;
        }

        private void SetElementFromMarketActivityRecordValue(string elementName, string value)
        {
            GetMarketActivityRecordChildElement(elementName).Value = value;
        }

        private XElement GetMarketActivityRecordChildElement(string elementName)
        {
            return _document.Root?.Element(_xmlNamespace + "MktActivityRecord")?.Element(_xmlNamespace + elementName)!;
        }
    }
}
