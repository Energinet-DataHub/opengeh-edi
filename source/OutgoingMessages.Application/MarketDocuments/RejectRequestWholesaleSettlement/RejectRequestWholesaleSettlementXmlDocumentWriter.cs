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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.DocumentWriters.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestWholesaleSettlement;

public class RejectRequestWholesaleSettlementXmlDocumentWriter : DocumentWriter
{
    public RejectRequestWholesaleSettlementXmlDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
                "RejectRequestWholesaleSettlement_MarketDocument",
                "urn:ediel.org:measure:rejectrequestwholesalesettlement:0:1 urn-ediel-org-measure-rejectrequestwholesalesettlement-0-1.xsd",
                "urn:ediel.org:measure:rejectrequestwholesalesettlement:0:1",
                "cim",
                "ERR"),
            parser,
            ReasonCode.FullyRejected.Code)
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.RejectRequestWholesaleSettlement == documentType;
    }

    protected override async Task WriteMarketActivityRecordsAsync(
        IReadOnlyCollection<string> marketActivityPayloads,
        XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var wholesaleServicesRecord in ParseFrom<RejectedWholesaleServicesRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "mRID",
                    null,
                    wholesaleServicesRecord.TransactionId.ToString())
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "originalTransactionIDReference_Series.mRID",
                    null,
                    wholesaleServicesRecord.OriginalTransactionIdReference)
                .ConfigureAwait(false);

            foreach (var reason in wholesaleServicesRecord.RejectReasons)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Reason", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "code", null, reason.ErrorCode)
                    .ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "text", null, reason.ErrorMessage)
                    .ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
