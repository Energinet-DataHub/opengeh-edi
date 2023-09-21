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
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common.Xml;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common.Xml;

internal static class EbixHeaderWriter
{
    internal static async Task WriteAsync(XmlWriter writer, MessageHeader messageHeader, DocumentDetails documentDetails, string? reasonCode, string? processType)
    {
        if (messageHeader == null) throw new ArgumentNullException(nameof(messageHeader));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        if (documentDetails == null) throw new ArgumentNullException(nameof(documentDetails));

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);
        await writer.WriteStartElementAsync(
            documentDetails.Prefix,
            documentDetails.Type,
            documentDetails.XmlNamespace).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "HeaderEnergyDocument", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "Identification", null, messageHeader.MessageId).ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, "DocumentType", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        writer.WriteValue("E31");
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

        await writer.WriteEndElementAsync().ConfigureAwait(false);  // end HeaderEnergyDocument

        await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessEnergyContext", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyBusinessProcess", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
        writer.WriteValue(EbixCode.Of(BusinessReason.From(messageHeader.BusinessReason)));
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyBusinessProcessRole", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
        writer.WriteValue(EbixCode.Of(EnumerationType.FromName<MarketRole>(messageHeader.SenderRole)));
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "EnergyIndustryClassification", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
        writer.WriteValue(GeneralValues.SectorTypeCode);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        if (processType != null)
        {
            await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessVariant", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            writer.WriteValue(processType);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false); // end ProcessEnergyContext
    }
}
