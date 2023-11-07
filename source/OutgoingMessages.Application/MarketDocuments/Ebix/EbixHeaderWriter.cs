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
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Ebix;

internal static class EbixHeaderWriter
{
    internal static async Task WriteAsync(XmlWriter writer, OutgoingMessageHeader messageHeader, DocumentDetails documentDetails, string? reasonCode, SettlementVersion? settlementVersion)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (documentDetails == null) throw new ArgumentNullException(nameof(documentDetails));

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);

        // Write Messageconatiner
        await writer.WriteStartElementAsync(null, "MessageContainer", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(null, "MessageReference", "urn:www:datahub:dk:b2b:v01", $"ENDK_{Guid.NewGuid():N}").ConfigureAwait(false);
        await writer.WriteElementStringAsync(null, "DocumentType", "urn:www:datahub:dk:b2b:v01", $"{documentDetails.Type.Replace("DK_", string.Empty, StringComparison.InvariantCultureIgnoreCase)}").ConfigureAwait(false);
        await writer.WriteElementStringAsync(null, "MessageType", "urn:www:datahub:dk:b2b:v01", "XML").ConfigureAwait(false);
        await writer.WriteStartElementAsync(null, "Payload", "urn:www:datahub:dk:b2b:v01").ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, documentDetails.Type, documentDetails.XmlNamespace).ConfigureAwait(false);

        // Begin HeaderEnergyDocument
        await writer.WriteStartElementAsync(documentDetails.Prefix, "HeaderEnergyDocument", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "Identification", null, messageHeader.MessageId).ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, "DocumentType", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        writer.WriteValue(documentDetails.TypeCode);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteElementStringAsync(documentDetails.Prefix, "Creation", null, messageHeader.TimeStamp.ToString()).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "SenderEnergyParty", null).ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, "Identification", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
        writer.WriteValue(messageHeader.SenderId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "RecipientEnergyParty", null).ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, "Identification", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
        writer.WriteValue(messageHeader.ReceiverId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End HeaderEnergyDocument

        // Begin ProcessEnergyContext
        await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessEnergyContext", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyBusinessProcess", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
        writer.WriteValue(EbixCode.Of(BusinessReason.FromName(messageHeader.BusinessReason)));
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyBusinessProcessRole", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        writer.WriteValue(EbixCode.Of(EnumerationType.FromName<MarketRole>(messageHeader.ReceiverRole)));
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyIndustryClassification", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
        writer.WriteValue(GeneralValues.SectorTypeCode);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        if (settlementVersion is not null)
        {
            await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessVariant", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            writer.WriteValue(EbixCode.Of(settlementVersion));
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End ProcessEnergyContext
    }
}
