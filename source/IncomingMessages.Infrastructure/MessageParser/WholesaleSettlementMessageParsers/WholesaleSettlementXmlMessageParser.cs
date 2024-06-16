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
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.WholesaleSettlementMessageParsers;

public class WholesaleSettlementXmlMessageParser : XmlBaseParser
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestWholesaleSettlement_MarketDocument";

    public WholesaleSettlementXmlMessageParser(CimXmlSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestWholesaleSettlement;

    protected override async Task<IncomingMarketMessageParserResult> ParseXmlDataAsync(
        XmlReader reader)
    {
        var root = await reader.ReadRootElementAsync().ConfigureAwait(false);
        var header = await MessageHeaderExtractor
            .ExtractAsync(reader, root, HeaderElementName, SeriesElementName)
            .ConfigureAwait(false);

        var listOfSeries = new List<RequestWholesaleServicesSeries>();
        await foreach (var series in ParseSeriesAsync(reader, root))
        {
            listOfSeries.Add(series);
        }

        return new IncomingMarketMessageParserResult(new RequestWholesaleServicesMessage(
            header.SenderId,
            header.SenderRole,
            header.ReceiverId,
            header.ReceiverRole,
            header.BusinessReason,
            header.MessageType,
            header.MessageId,
            header.CreatedAt,
            header.BusinessType,
            listOfSeries.AsReadOnly()));
    }

    private async IAsyncEnumerable<RequestWholesaleServicesSeries> ParseSeriesAsync(
        XmlReader reader,
        RootElement rootElement)
    {
        var id = string.Empty;
        var startDateAndOrTimeDateTime = string.Empty;
        string? endDateAndOrTimeDateTime = null;
        string? gridArea = null;
        string? energySupplierId = null;
        string? chargeTypeOwnerId = null;
        string? settlementVersion = null;
        string? resolution = null;
        string? chargeType = null;
        string? chargeId = null;
        List<RequestWholesaleServicesChargeType> chargeTypes = new();
        var ns = rootElement.DefaultNamespace;

        await reader.AdvanceToAsync(SeriesElementName, ns).ConfigureAwait(false);

        while (!reader.EOF)
        {
            if (reader.Is(SeriesElementName, ns, XmlNodeType.EndElement))
            {
                var series = new RequestWholesaleServicesSeries(
                    id,
                    startDateAndOrTimeDateTime,
                    endDateAndOrTimeDateTime,
                    gridArea,
                    energySupplierId,
                    settlementVersion,
                    resolution,
                    chargeTypeOwnerId,
                    // clones the list
                    chargeTypes.Select(a => a).ToList());

                id = string.Empty;
                startDateAndOrTimeDateTime = string.Empty;
                endDateAndOrTimeDateTime = null;
                gridArea = null;
                energySupplierId = null;
                chargeTypeOwnerId = null;
                settlementVersion = null;
                resolution = null;
                chargeType = null;
                chargeId = null;
                chargeTypes.Clear();
                yield return series;
            }

            if (reader.NodeType == XmlNodeType.Element && reader.SchemaInfo?.Validity == XmlSchemaValidity.Invalid)
                await reader.ReadToEndAsync().ConfigureAwait(false);

            if (reader.Depth == 2 && reader.Is("mRID", ns))
            {
                id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
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
                gridArea = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("energySupplier_MarketParticipant.mRID", ns))
            {
                energySupplierId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("chargeTypeOwner_MarketParticipant.mRID", ns))
            {
                chargeTypeOwnerId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("settlement_Series.version", ns))
            {
                settlementVersion = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Is("aggregationSeries_Period.resolution", ns))
            {
                resolution = reader.ReadString();
            }
            else if (reader.Depth == 3 && reader.Is("type", ns))
            {
                chargeType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }
            else if (reader.Depth == 3 && reader.Is("mRID", ns))
            {
                chargeId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
            }

            // We're at the end of a charge type element
            else if (reader.Is("ChargeType", ns, XmlNodeType.EndElement))
            {
                // chargeId and chargeType are read, since these lives inside the ChargeType element
                var chargeTypeOwner = new RequestWholesaleServicesChargeType(chargeId, chargeType);
                chargeTypes.Add(chargeTypeOwner);

                // Reset the current values, since we're done with this chargeType element
                chargeType = null;
                chargeId = null;

                // Move to next element
                await reader.ReadAsync().ConfigureAwait(false);
            }
            else
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }
}
