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
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers;

public class MeteredDateForMeasurementPointXmlMessageParser(CimXmlSchemaProvider schemaProvider) : XmlMessageParserBase(schemaProvider)
{
    private const string SeriesElementName = "Series";
    private const string MridElementName = "mRID";
    private const string UnitTypeElementName = "quantity_Measure_Unit.name";
    private const string ProductElementName = "product";
    private const string TypeOfMeteringPointElementName = "marketEvaluationPoint.type";
    private const string MeteringPointDomainLocationElementName = "marketEvaluationPoint.mRID";
    private const string PeriodElementName = "Period";
    private const string ResolutionElementName = "resolution";
    private const string TimeIntervalElementName = "timeInterval";
    private const string StartElementName = "start";
    private const string EndElementName = "end";
    private const string PointElementName = "Point";
    private const string PositionElementName = "position";
    private const string QuantityElementName = "quantity";
    private const string QualityElementName = "quality";

    protected override string RootPayloadElementName => "NotifyValidatedMeasureData_MarketDocument";

    protected override IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(XDocument document, XNamespace ns, string senderNumber)
    {
        var seriesElements = document.Descendants(ns + SeriesElementName);
        var result = new List<MeteredDataForMeasurementPointSeries>();

        foreach (var seriesElement in seriesElements)
        {
            var id = seriesElement.Element(ns + MridElementName)?.Value ?? string.Empty;
            var meteringPointLocationId = seriesElement.Element(ns + MeteringPointDomainLocationElementName)?.Value;
            var meteringPointType = seriesElement.Element(ns + TypeOfMeteringPointElementName)?.Value;
            var productNumber = seriesElement.Element(ns + ProductElementName)?.Value;
            var productUnitType = seriesElement.Element(ns + UnitTypeElementName)?.Value;

            var periodElement = seriesElement.Element(ns + PeriodElementName);
            var resolution = periodElement?.Element(ns + ResolutionElementName)?.Value;
            var startDateAndOrTimeDateTime =
                periodElement
                    ?.Element(ns + TimeIntervalElementName)
                    ?.Element(ns + StartElementName)
                    ?.Value ?? string.Empty;
            var endDateAndOrTimeDateTime = periodElement
                ?.Element(ns + TimeIntervalElementName)
                ?.Element(ns + EndElementName)
                ?.Value;

            var energyObservations = seriesElement
                //.Element(ns + Period)?
                .Descendants(ns + PointElementName)
                .Select(
                    e => new EnergyObservation(
                        e.Element(ns + PositionElementName)?.Value,
                        e.Element(ns + QuantityElementName)?.Value,
                        e.Element(ns + QualityElementName)?.Value))
                .ToList();

            result.Add(
                new MeteredDataForMeasurementPointSeries(
                    id,
                    resolution,
                    startDateAndOrTimeDateTime,
                    endDateAndOrTimeDateTime,
                    productNumber,
                    productUnitType,
                    meteringPointType,
                    meteringPointLocationId,
                    senderNumber,
                    energyObservations));
        }

        return result.AsReadOnly();
    }

    protected override IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions)
    {
        return new IncomingMarketMessageParserResult(new MeteredDataForMeasurementPointMessage(
            header.MessageId,
            header.MessageType,
            header.CreatedAt,
            header.SenderId,
            header.ReceiverId,
            header.SenderRole,
            header.BusinessReason,
            header.ReceiverRole,
            header.BusinessType,
            transactions));
    }
}
