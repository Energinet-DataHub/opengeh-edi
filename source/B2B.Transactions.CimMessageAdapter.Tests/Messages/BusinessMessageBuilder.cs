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

namespace B2B.CimMessageAdapter.Tests.Messages
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
            return new BusinessMessageBuilder("Messages\\ValidRequestChangeOfSupplier.xml", "urn:ediel.org:structure:requestchangeofsupplier:0:1");
        }

        public BusinessMessageBuilder WithSenderRole(string roleType)
        {
            _document.Root!
                .Element(_xmlNamespace + "sender_MarketParticipant.marketRole.type")!.Value = roleType;
            return this;
        }

        public Stream Message()
        {
            var message = new MemoryStream();
            _document.Save(message);
            message.Position = 0;
            return message;
        }
    }
}
