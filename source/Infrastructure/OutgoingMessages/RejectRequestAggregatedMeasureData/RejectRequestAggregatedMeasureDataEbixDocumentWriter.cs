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
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common.Xml;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common.Ebix;
using Point = Energinet.DataHub.EDI.Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.AggregationResult;

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
            parser,
            EbixCode.Of(ReasonCode.FullyRejected))
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        return DocumentType.RejectRequestAggregatedMeasureData == documentType;
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var rejectedTimeSerie in ParseFrom<RejectedTimeSerie>(marketActivityPayloads))
        {
            if (rejectedTimeSerie.RejectReasons.Count == 0)
                throw new NotSupportedException("Unable to create reject message if no reason is supplied");

            // Begin PayloadResponseEvent
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadResponseEvent", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Identification", null, rejectedTimeSerie.TransactionId.ToString("N")).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "StatusType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(ReasonCode.FullyRejected)).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ResponseReasonType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(rejectedTimeSerie.RejectReasons[0].ErrorCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "OriginalBusinessDocument", null, rejectedTimeSerie.OriginalTransactionIdReference).ConfigureAwait(false);

            // End PayloadResponseEvent
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private Task WriteQualityIfRequiredAsync(XmlWriter writer, Point point)
    {
        if (point.Quality is null)
            return Task.CompletedTask;

        return writer.WriteElementStringAsync(DocumentDetails.Prefix, "QuantityQuality", null, EbixCode.Of(Quality.From(point.Quality)));
    }
}
