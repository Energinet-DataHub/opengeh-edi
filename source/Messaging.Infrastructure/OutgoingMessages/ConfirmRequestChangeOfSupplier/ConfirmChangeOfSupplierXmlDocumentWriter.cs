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
using Messaging.Application.Common;
using Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;

namespace Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmChangeOfSupplierXmlDocumentWriter : DocumentWriter
{
    public ConfirmChangeOfSupplierXmlDocumentWriter(IMarketActivityRecordParser parser)
    : base(
        new DocumentDetails(
            "ConfirmRequestChangeOfSupplier_MarketDocument",
            "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd",
            "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1",
            "cim",
            "414"),
        parser)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, marketActivityRecord.Id.ToString())
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "originalTransactionIDReference_MktActivityRecord.mRID",
                null,
                marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(marketActivityRecord.MarketEvaluationPointId);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
