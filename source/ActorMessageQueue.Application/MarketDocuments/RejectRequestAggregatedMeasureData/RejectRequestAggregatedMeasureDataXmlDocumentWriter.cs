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
using Energinet.DataHub.EDI.ActorMessageQueue.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.MarketDocuments;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.OutgoingMessages;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.MarketDocuments.RejectRequestAggregatedMeasureData;

public class RejectRequestAggregatedMeasureDataXmlDocumentWriter : DocumentWriter
{
    public RejectRequestAggregatedMeasureDataXmlDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
                "RejectRequestAggregatedMeasureData_MarketDocument",
                "urn:ediel.org:measure:rejectrequestaggregatedmeasuredata:0:1 urn-ediel-org-measure-rejectrequestaggregatedmeasuredata-0-1.xsd",
                "urn:ediel.org:measure:rejectrequestaggregatedmeasuredata:0:1",
                "cim",
                "ERR"),
            parser,
            CimCode.Of(ReasonCode.FullyRejected))
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        return DocumentType.RejectRequestAggregatedMeasureData == documentType;
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        foreach (var rejectedTimeSerie in ParseFrom<RejectedTimeSerie>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, rejectedTimeSerie.TransactionId.ToString())
                 .ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "originalTransactionIDReference_Series.mRID",
                null,
                rejectedTimeSerie.OriginalTransactionIdReference).ConfigureAwait(false);

            foreach (var reason in rejectedTimeSerie.RejectReasons)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Reason", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, reason.ErrorCode).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "text", null, reason.ErrorMessage).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
