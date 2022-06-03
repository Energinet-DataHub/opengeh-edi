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
using Messaging.Application.Common;

namespace Messaging.Application.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmChangeOfSupplierDocumentWriter
{
    private const string Prefix = "cim";
    private const string DocumentType = "ConfirmRequestChangeOfSupplier_MarketDocument";
    private const string XmlNamespace = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1";
    private const string SchemaLocation = "urn:ediel.org:structure:confirmrequestchangeofsupplier:0:1 urn-ediel-org-structure-confirmrequestchangeofsupplier-0-1.xsd";
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public ConfirmChangeOfSupplierDocumentWriter(IMarketActivityRecordParser marketActivityRecordParser)
    {
        _marketActivityRecordParser = marketActivityRecordParser;
        DocumentDetails = new DocumentDetails(DocumentType, SchemaLocation, XmlNamespace, Prefix);
    }

    public DocumentDetails DocumentDetails { get; }

    public Task WriteAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        return WriteMarketActivityRecordsAsync(GetMarketActivityRecordsFrom(marketActivityPayloads), writer);
    }

    private static async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<MarketActivityRecord> marketActivityRecords, XmlWriter writer)
    {
        foreach (var marketActivityRecord in marketActivityRecords)
        {
            await writer.WriteStartElementAsync(Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "mRID", null, marketActivityRecord.Id.ToString())
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                Prefix,
                "originalTransactionIDReference_MktActivityRecord.mRID",
                null,
                marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            await writer.WriteStartElementAsync(Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(marketActivityRecord.MarketEvaluationPointId);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private List<MarketActivityRecord> GetMarketActivityRecordsFrom(IReadOnlyCollection<string> marketActivityPayloads)
    {
        return marketActivityPayloads
            .Select(payload => _marketActivityRecordParser.From<MarketActivityRecord>(payload))
            .ToList();
    }
}
