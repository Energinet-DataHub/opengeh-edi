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

using System.IO;
using System.Xml.Linq;

namespace B2B.Transactions.IntegrationTests.CimMessageAdapter.Messages
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

        public static BusinessMessageBuilder RequestChangeOfSupplier()
        {
            return new BusinessMessageBuilder("CimMessageAdapter//Messages//RequestChangeOfSupplier.xml", "urn:ediel.org:structure:requestchangeofsupplier:0:1");
        }

        public BusinessMessageBuilder WithSenderRole(string roleType)
        {
            SetRootChildElementValue("sender_MarketParticipant.marketRole.type", roleType);
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

        public Stream Message()
        {
            var message = new MemoryStream();
            _document.Save(message);
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

        private void SetRootChildElementValue(string elementName, string value)
        {
            _document.Root!
                .Element(_xmlNamespace + elementName)!.Value = value;
        }
    }
}
