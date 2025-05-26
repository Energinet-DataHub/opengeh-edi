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

using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;

public class AcknowledgementEbixDocumentWriter(IMessageRecordParser parser)
    : EbixDocumentWriter(new DocumentDetails(
        "DK_Acknowledgement",
        string.Empty,
        "un:unece:260:data:EEM-DK_Acknowledgement:v3",
        "ns0",
        "294"),
        parser)
{
    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.Acknowledgement == documentType;
    }

    protected override async Task WriteHeaderAsync(
        OutgoingMessageHeader header,
        DocumentDetails documentDetails,
        XmlWriter writer,
        SettlementVersion? settlementVersion)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documentDetails);

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);

        // Write Messageconatiner
        await writer.WriteStartElementAsync(null, "MessageContainer", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(
                null,
                "MessageReference",
                "urn:www:datahub:dk:b2b:v01",
                $"ENDK_{Guid.NewGuid():N}")
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(
                null,
                "DocumentType",
                "urn:www:datahub:dk:b2b:v01",
                $"{documentDetails.Type.Replace("DK_", string.Empty, StringComparison.InvariantCultureIgnoreCase)}")
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(null, "MessageType", "urn:www:datahub:dk:b2b:v01", "XML")
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(null, "Payload", "urn:www:datahub:dk:b2b:v01").ConfigureAwait(false);
        await writer.WriteStartElementAsync(documentDetails.Prefix, documentDetails.Type, documentDetails.XmlNamespace)
            .ConfigureAwait(false);

        // Begin HeaderEnergyDocument
        await writer.WriteStartElementAsync(documentDetails.Prefix, "HeaderEnergyDocument", null).ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "Identification", null, header.MessageId)
            .ConfigureAwait(false);
        await WriteCodeWithCodeListReferenceAttributesAsync("DocumentType", documentDetails.TypeCode, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(
                documentDetails.Prefix,
                "Creation",
                null,
                header.TimeStamp.ToString())
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "SenderEnergyParty", null).ConfigureAwait(false);
        await WriteActorNumberWithAttributeAsync(ActorNumber.Create(header.SenderId), writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "RecipientEnergyParty", null).ConfigureAwait(false);
        await WriteActorNumberWithAttributeAsync(ActorNumber.Create(header.ReceiverId), writer).ConfigureAwait(false);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End HeaderEnergyDocument

        // Begin ProcessEnergyContext
        await writer.WriteStartElementAsync(documentDetails.Prefix, "ProcessEnergyContext", null).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync("EnergyBusinessProcess", EbixCode.Of(BusinessReason.FromName(header.BusinessReason)), writer).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync("EnergyBusinessProcessRole", EbixCode.Of(ActorRole.FromCode(header.ReceiverRole)), writer).ConfigureAwait(false);

        await WriteCodeWithCodeListReferenceAttributesAsync("EnergyIndustryClassification", GeneralValues.SectorTypeCode, writer).ConfigureAwait(false);

        await writer.WriteElementStringAsync(documentDetails.Prefix, "OriginalBusinessMessage", null, header.RelatedToMessageId!)
            .ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
        // End ProcessEnergyContext
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var rejectedForwardMeteredDataRecord in ParseFrom<RejectedForwardMeteredDataRecord>(marketActivityPayloads))
        {
            if (rejectedForwardMeteredDataRecord.RejectReasons.Count == 0)
            {
                throw new NotSupportedException("Unable to create reject message if no reason is supplied");
            }

            // Ebix only support one reject reason, hence we only take the first one
            var firstRejectReason = rejectedForwardMeteredDataRecord.RejectReasons.First();

            // Begin PayloadResponseEvent
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadResponseEvent", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Identification",
                    null,
                    rejectedForwardMeteredDataRecord.TransactionId.Value)
                .ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "StatusType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(ReasonCode.FullyRejected)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ResponseReasonType", null)
                .ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteStringAsync(firstRejectReason.ErrorCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "ReasonText", null, firstRejectReason.ErrorMessage)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "OriginalBusinessDocument", null, rejectedForwardMeteredDataRecord.OriginalTransactionIdReference.Value)
                .ConfigureAwait(false);
            // End PayloadResponseEvent
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
