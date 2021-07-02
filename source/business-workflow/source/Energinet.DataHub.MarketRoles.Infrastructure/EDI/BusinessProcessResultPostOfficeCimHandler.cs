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
using System.Xml.Serialization;
using Energinet.DataHub.MarketRoles.Application.Common;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI
{
    public abstract class BusinessProcessResultPostOfficeCimHandler<TBusinessRequest> : BusinessProcessResultHandler<TBusinessRequest>
        where TBusinessRequest : IBusinessRequest
    {
        protected BusinessProcessResultPostOfficeCimHandler(
            IOutbox outbox,
            IOutboxMessageFactory outboxMessageFactory)
            : base(outbox, outboxMessageFactory)
        {
        }

        protected static string Serialize<TObject>(TObject toSerialize, string cimNamespace)
        {
            if (toSerialize == null) throw new ArgumentNullException(nameof(toSerialize));

            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            var xmlNamespaces = new XmlSerializerNamespaces();
            xmlNamespaces.Add("cim", cimNamespace);
            xmlNamespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            using (var writer = new Utf8StringWriter())
            {
                xmlSerializer.Serialize(writer, toSerialize, xmlNamespaces);
                return writer.ToString();
            }
        }

        protected static PostOfficeEnvelope CreatePostOfficeEnvelope(string recipient, string cimContent, string messageType)
        {
            return new(
                Id: Guid.NewGuid().ToString(),
                Recipient: recipient,
                Content: cimContent,
                MessageType: messageType);
        }
    }
}
