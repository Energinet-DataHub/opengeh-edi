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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM016;

public class AggregatedMeasureDataXmlMessageParser(CimXmlSchemaProvider schemaProvider) : XmlMessageParserBase(schemaProvider)
{
    private const string MridElementName = "mRID";
    private const string MarketEvaluationPointTypeElementName = "marketEvaluationPoint.type";
    private const string MarketEvaluationPointSettlementMethodElementName = "marketEvaluationPoint.settlementMethod";
    private const string StartElementName = "start_DateAndOrTime.dateTime";
    private const string EndElementName = "end_DateAndOrTime.dateTime";
    private const string GridAreaElementName = "meteringGridArea_Domain.mRID";
    private const string EnergySupplierNumberElementName = "energySupplier_MarketParticipant.mRID";
    private const string BalanceResponsibleNumberElementName = "balanceResponsibleParty_MarketParticipant.mRID";
    private const string SettlementVersionElementName = "settlement_Series.version";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    protected override string RootPayloadElementName => "RequestAggregatedMeasureData_MarketDocument";

    protected override IIncomingMessageSeries ParseTransaction(XElement seriesElement, XNamespace ns, string senderNumber)
    {
        var id = seriesElement.Element(ns + MridElementName)?.Value ?? string.Empty;
        var marketEvaluationPointType = seriesElement.Element(ns + MarketEvaluationPointTypeElementName)?.Value;
        var marketEvaluationSettlementMethod = seriesElement.Element(ns + MarketEvaluationPointSettlementMethodElementName)?.Value;
        var startDateAndOrTimeDateTime = seriesElement.Element(ns + StartElementName)?.Value ?? string.Empty;
        var endDateAndOrTimeDateTime = seriesElement.Element(ns + EndElementName)?.Value;
        var meteringGridAreaDomainId = seriesElement.Element(ns + GridAreaElementName)?.Value;
        var energySupplierMarketParticipantId = seriesElement.Element(ns + EnergySupplierNumberElementName)?.Value;
        var balanceResponsiblePartyMarketParticipantId = seriesElement.Element(ns + BalanceResponsibleNumberElementName)?.Value;
        var settlementVersion = seriesElement.Element(ns + SettlementVersionElementName)?.Value;

        return new RequestAggregatedMeasureDataMessageSeries(
            id,
            marketEvaluationPointType,
            marketEvaluationSettlementMethod,
            startDateAndOrTimeDateTime,
            endDateAndOrTimeDateTime,
            meteringGridAreaDomainId,
            energySupplierMarketParticipantId,
            balanceResponsiblePartyMarketParticipantId,
            settlementVersion);
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(
            new RequestAggregatedMeasureDataMessage(
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
