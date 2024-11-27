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

using System.Xml.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.WholesaleSettlementMessageParsers;

public class WholesaleSettlementXmlMessageParser(CimXmlSchemaProvider schemaProvider) : XmlMessageParserBase(schemaProvider)
{
    private const string SeriesElementName = "Series";
    private const string MridElementName = "mRID";
    private const string StartElementName = "start_DateAndOrTime.dateTime";
    private const string EndElementName = "end_DateAndOrTime.dateTime";
    private const string GridAreaElementName = "meteringGridArea_Domain.mRID";
    private const string EnergySupplierElementName = "energySupplier_MarketParticipant.mRID";
    private const string ChargeOwnerElementName = "chargeTypeOwner_MarketParticipant.mRID";
    private const string SettlementVersionElementName = "settlement_Series.version";
    private const string ResolutionElementName = "aggregationSeries_Period.resolution";
    private const string ChargeElementName = "ChargeType";
    private const string ChargeTypeElementName = "type";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestWholesaleSettlement;

    protected override string RootPayloadElementName => "RequestWholesaleSettlement_MarketDocument";

    protected override IIncomingMessageSeries ParseTransaction(XElement seriesElement, XNamespace ns, string senderNumber)
    {
        var id = seriesElement.Element(ns + MridElementName)?.Value ?? string.Empty;
        var startDateAndOrTimeDateTime = seriesElement.Element(ns + StartElementName)?.Value ?? string.Empty;
        var endDateAndOrTimeDateTime = seriesElement.Element(ns + EndElementName)?.Value;
        var gridArea = seriesElement.Element(ns + GridAreaElementName)?.Value;
        var energySupplierId = seriesElement.Element(ns + EnergySupplierElementName)?.Value;
        var chargeTypeOwnerId = seriesElement.Element(ns + ChargeOwnerElementName)?.Value;
        var settlementVersion = seriesElement.Element(ns + SettlementVersionElementName)?.Value;
        var resolution = seriesElement.Element(ns + ResolutionElementName)?.Value;

        var chargeTypes = seriesElement.Descendants(ns + ChargeElementName)
            .Select(e => new RequestWholesaleServicesChargeType(
                e.Element(ns + MridElementName)?.Value,
                e.Element(ns + ChargeTypeElementName)?.Value));

        return new RequestWholesaleServicesSeries(
            id,
            startDateAndOrTimeDateTime,
            endDateAndOrTimeDateTime,
            gridArea,
            energySupplierId,
            settlementVersion,
            resolution,
            chargeTypeOwnerId,
            chargeTypes.Select(a => a).ToList());
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
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
            transactions));
    }
}
