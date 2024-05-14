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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestAggregatedMeasureData;

public class RejectRequestAggregatedMeasureDataEbixDocumentWriter : EbixDocumentWriter
{
    public RejectRequestAggregatedMeasureDataEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "DK_RejectRequestMeteredDataAggregated",
            string.Empty,
            "un:unece:260:data:EEM-DK_RejectRequestMeteredDataAggregated:v3",
            "ns0",
            "ERR"),
            parser)
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.RejectRequestAggregatedMeasureData == documentType;
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var rejectedTimeSerie in ParseFrom<RejectedTimeSerieMarketActivityRecord>(marketActivityPayloads))
        {
            if (rejectedTimeSerie.RejectReasons.Count == 0)
                throw new NotSupportedException("Unable to create reject message if no reason is supplied");

            // Begin PayloadResponseEvent
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadResponseEvent", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Identification",
                    null,
                    TransactionIdToEbixString(rejectedTimeSerie.TransactionId))
                .ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "StatusType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(ReasonCode.FullyRejected)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ResponseReasonType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(rejectedTimeSerie.RejectReasons.First().ErrorCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "OriginalBusinessDocument",
                    null,
                    TransactionIdToEbixString(rejectedTimeSerie.OriginalTransactionIdReference))
                .ConfigureAwait(false);

            // End PayloadResponseEvent
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
