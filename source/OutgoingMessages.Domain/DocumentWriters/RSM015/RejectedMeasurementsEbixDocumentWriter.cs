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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestWholesaleSettlement;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM015;

public class RejectedMeasurementsEbixDocumentWriter : EbixDocumentWriter
{
    public RejectedMeasurementsEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
                "DK_RejectRequestMeteredDataValidated",
                string.Empty,
                "un:unece:260:data:EEM-DK_RejectRequestMeteredDataValidated:v3",
                "ns0",
                "ERR"),
            parser)
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.RejectRequestMeasurements == documentType;
    }

    protected override async Task WriteMarketActivityRecordsAsync(
        IReadOnlyCollection<string> marketActivityPayloads,
        XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var rejectedMeasurements in ParseFrom<RejectedMeasurementsRecord>(marketActivityPayloads))
        {
            if (rejectedMeasurements.RejectReasons.Count == 0)
            {
                throw new NotSupportedException("Unable to create reject message if no reason is supplied");
            }

            // Begin PayloadResponseEvent
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadResponseEvent", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Identification",
                    null,
                    rejectedMeasurements.TransactionId.Value)
                .ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "StatusType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(ReasonCode.FullyRejected)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ResponseReasonType", null)
                .ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(rejectedMeasurements.RejectReasons.First().ErrorCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringPointDomainLocation", null).ConfigureAwait(false);
            await WriteMeteringPointIdAsync(rejectedMeasurements.MeteringPointId, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "OriginalBusinessDocument",
                    null,
                    rejectedMeasurements.OriginalTransactionIdReference.Value)
                .ConfigureAwait(false);

            // End PayloadResponseEvent
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
