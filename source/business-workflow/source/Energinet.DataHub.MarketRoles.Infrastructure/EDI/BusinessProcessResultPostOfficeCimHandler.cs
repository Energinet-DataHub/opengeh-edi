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
