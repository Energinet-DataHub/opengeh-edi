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
using System.Xml.Schema;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.Factories;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.AggregatedMeasureDataRequestMessageParsers;

public class AggregatedMeasureDataXmlMessageParser : XmlBaseParser
{
    private const string SeriesRecordElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";

    public AggregatedMeasureDataXmlMessageParser(CimXmlSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    protected override async Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var messageHeader = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesRecordElementName)
            .ConfigureAwait(false);

        var series = new List<RequestAggregatedMeasureDataMessageSeries>();
        await foreach (var serie in ParseSeriesAsync(reader, root))
        {
            series.Add(serie);
        }

        return new IncomingMarketMessageParserResult(
            RequestAggregatedMeasureDataMessageFactory.Create(messageHeader, series.AsReadOnly()));
    }

    private static async IAsyncEnumerable<RequestAggregatedMeasureDataMessageSeries> ParseSeriesAsync(XmlReader reader, RootElement rootElement)
    {
        var id = string.Empty;
        var startDateAndOrTimeDateTime = string.Empty;
        string? marketEvaluationPointType = null;
        string? marketEvaluationSettlementMethod = null;
        string? endDateAndOrTimeDateTime = null;
        string? meteringGridAreaDomainId = null;
        string? energySupplierMarketParticipantId = null;
        string? balanceResponsiblePartyMarketParticipantId = null;
        string? settlementVersion = null;
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(SeriesRecordElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(SeriesRecordElementName, ns, XmlNodeType.EndElement))
            {
                var series = new RequestAggregatedMeasureDataMessageSeries(
                    id,
                    marketEvaluationPointType,
                    marketEvaluationSettlementMethod,
                    startDateAndOrTimeDateTime,
                    endDateAndOrTimeDateTime,
                    meteringGridAreaDomainId,
                    energySupplierMarketParticipantId,
                    balanceResponsiblePartyMarketParticipantId,
                    settlementVersion);

                id = string.Empty;
                startDateAndOrTimeDateTime = string.Empty;
                marketEvaluationPointType = null;
                marketEvaluationSettlementMethod = null;
                endDateAndOrTimeDateTime = null;
                meteringGridAreaDomainId = null;
                energySupplierMarketParticipantId = null;
                balanceResponsiblePartyMarketParticipantId = null;

                yield return series;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.type", ns))
            {
                marketEvaluationPointType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("marketEvaluationPoint.settlementMethod", ns))
            {
                marketEvaluationSettlementMethod = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("start_DateAndOrTime.dateTime", ns))
            {
                startDateAndOrTimeDateTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("end_DateAndOrTime.dateTime", ns))
            {
                endDateAndOrTimeDateTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("meteringGridArea_Domain.mRID", ns))
            {
                meteringGridAreaDomainId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("energySupplier_MarketParticipant.mRID", ns))
            {
                energySupplierMarketParticipantId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("balanceResponsibleParty_MarketParticipant.mRID", ns))
            {
                balanceResponsiblePartyMarketParticipantId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("settlement_Series.version", ns))
            {
                settlementVersion = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }
}
