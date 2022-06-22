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

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsDocumentWriter : DocumentWriter
{
    private const string Prefix = "cim";
    private const string DocumentType = "AccountingPointCharacteristics_MarketDocument";
    private const string XmlNamespace = "urn:ediel.org:structure:accountingpointcharacteristics:0:1";
    private const string SchemaLocation = "urn:ediel.org:structure:accountingpointcharacteristics:0:1 urn-ediel-org-structure-accountingpointcharacteristics-0-1.xsd";

    public AccountingPointCharacteristicsDocumentWriter(IMarketActivityRecordParser parser)
        : base(new DocumentDetails(DocumentType, SchemaLocation, XmlNamespace, Prefix), parser)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        if (marketActivityPayloads == null) throw new ArgumentNullException(nameof(marketActivityPayloads));
        if (writer == null) throw new ArgumentNullException(nameof(writer));
        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(Prefix, "MktActivityRecord", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "mRID", null, marketActivityRecord.Id.ToString())
                .ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                Prefix,
                "originalTransactionIDReference_MktActivityRecord.mRID",
                null,
                marketActivityRecord.OriginalTransactionId).ConfigureAwait(false);
            await writer.WriteElementStringAsync(Prefix, "validityStart_DateAndOrTime.dateTime", null, marketActivityRecord.ValidityStartDate.ToString())
                .ConfigureAwait(false);
            await writer.WriteStartElementAsync(Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(marketActivityRecord.Id);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await WriteMarketEvaluationPointAsync(marketActivityRecord.MarketEvaluationPt, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private static async Task WriteMarketEvaluationPointAsync(MarketEvaluationPoint marketEvaluationPoint, XmlWriter writer)
    {
        await writer.WriteStartElementAsync(Prefix, "MarketEvaluationPoint", null).ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.Id);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "meteringPointResponsible_MarketParticipant.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.MeteringPointResponsible);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteElementStringAsync(Prefix, "type", null, marketEvaluationPoint.Type).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "settlementMethod", null, marketEvaluationPoint.SettlementMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "meteringMethod", null, marketEvaluationPoint.MeteringMethod).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "connectionState", null, marketEvaluationPoint.ConnectionState).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "readCycle", null, marketEvaluationPoint.ReadCycle).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "netSettlementGroup", null, marketEvaluationPoint.NetSettlementGroup).ConfigureAwait(false);
        await writer.WriteElementStringAsync(Prefix, "nextReadingDate", null, marketEvaluationPoint.NextReadingDate).ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.MeteringGridAreaId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "inMeteringGridArea_Domain.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.InMeteringGridAreaId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "outMeteringGridArea_Domain.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.OutMeteringGridAreaId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteStartElementAsync(Prefix, "linked_MarketEvaluationPoint.mRID", null).ConfigureAwait(false);
        await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
        writer.WriteValue(marketEvaluationPoint.LinkedMarketEvaluationPointId);
        await writer.WriteEndElementAsync().ConfigureAwait(false);

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
