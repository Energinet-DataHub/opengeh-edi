﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;

public sealed class AcknowledgementXmlDocumentWriter(IMessageRecordParser parser)
    : CimXmlDocumentWriter(
          new DocumentDetails(
              "Acknowledgement_MarketDocument",
              "urn:ediel.org:general:acknowledgement:0:1 urn-ediel-org-general-acknowledgement-0-1.xsd",
              "urn:ediel.org:general:acknowledgement:0:1",
              "cim",
              string.Empty),
          parser)
{
    protected override async Task WriteHeaderAsync(OutgoingMessageHeader messageHeader, DocumentDetails documentDetails, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(messageHeader);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(documentDetails);

        await writer.WriteStartDocumentAsync().ConfigureAwait(false);
        await writer.WriteStartElementAsync(
            documentDetails.Prefix,
            documentDetails.Type,
            documentDetails.XmlNamespace).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance")
            .ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(
                "xsi",
                "schemaLocation",
                null,
                documentDetails.SchemaLocation)
            .ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "mRID", null, messageHeader.MessageId).ConfigureAwait(false);
        await writer.WriteElementStringAsync(documentDetails.Prefix, "businessSector.type", null, GeneralValues.SectorTypeCode).ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "sender_MarketParticipant.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.SenderId))).ConfigureAwait(false);
        writer.WriteValue(messageHeader.SenderId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer.WriteElementStringAsync(
                documentDetails.Prefix,
                "sender_MarketParticipant.marketRole.type",
                null,
                ActorRole.FromCode(messageHeader.SenderRole).Code)
            .ConfigureAwait(false);

        await writer.WriteStartElementAsync(documentDetails.Prefix, "receiver_MarketParticipant.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.ReceiverId))).ConfigureAwait(false);
        writer.WriteValue(messageHeader.ReceiverId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);
        await writer
            .WriteElementStringAsync(documentDetails.Prefix, "receiver_MarketParticipant.marketRole.type", null, messageHeader.ReceiverRole)
            .ConfigureAwait(false);

        await writer.WriteElementStringAsync(documentDetails.Prefix, "createdDateTime", null, messageHeader.TimeStamp.ToString()).ConfigureAwait(false);

        await writer.WriteElementStringAsync(documentDetails.Prefix, "received_MarketDocument.mRID", null, messageHeader.RelatedToMessageId!).ConfigureAwait(false);

        await writer.WriteElementStringAsync(documentDetails.Prefix, "received_MarketDocument.process.processType", null, BusinessReason.FromName(messageHeader.BusinessReason).Code)
            .ConfigureAwait(false);
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        var rejectedForwardMeteredDataRecords = ParseFrom<RejectedForwardMeteredDataRecord>(marketActivityPayloads);

        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
        foreach (var rejectedForwardMeteredDataRecord in rejectedForwardMeteredDataRecords)
        {
            await WriteElementAsync("mRID", rejectedForwardMeteredDataRecord.OriginalTransactionIdReference.Value, writer).ConfigureAwait(false);
            await WriteReasonElementsAsync(rejectedForwardMeteredDataRecord.RejectReasons, writer).ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteReasonElementsAsync(IReadOnlyCollection<RejectReason>? reasons, XmlWriter writer)
    {
        if (reasons == null || reasons.Count == 0)
            return;

        foreach (var reason in reasons)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Reason", null).ConfigureAwait(false);
            await WriteElementAsync("code", reason.ErrorCode, writer).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(reason.ErrorMessage))
            {
                await WriteElementAsync("text", reason.ErrorMessage, writer).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
