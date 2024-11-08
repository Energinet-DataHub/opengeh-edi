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
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Ebix;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers;

public class EbixMessageParser(EbixSchemaProvider schemaProvider) : EbixMessageParserBase(schemaProvider)
{
    private const string SeriesElementName = "PayloadEnergyTimeSeries";
    private const string Identification = "Identification";
    private const string ResolutionDuration = "ResolutionDuration";
    private const string ObservationTimeSeriesPeriod = "ObservationTimeSeriesPeriod";
    private const string Start = "Start";
    private const string End = "End";
    private const string IncludedProductCharacteristic = "IncludedProductCharacteristic";
    private const string UnitType = "UnitType";
    private const string DetailMeasurementMeteringPointCharacteristic = "DetailMeasurementMeteringPointCharacteristic";
    private const string TypeOfMeteringPoint = "TypeOfMeteringPoint";
    private const string MeteringPointDomainLocation = "MeteringPointDomainLocation";
    private const string Position = "Position";
    private const string EnergyQuantity = "EnergyQuantity";
    private const string QuantityQuality = "QuantityQuality";
    private const string IntervalEnergyObservation = "IntervalEnergyObservation";

    protected override string RootPayloadElementName => "DK_MeteredDataTimeSeries";

    protected override IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(XDocument document, XNamespace ns, string senderNumber)
    {
        var seriesElements = document.Descendants(ns + SeriesElementName);
        var result = new List<MeteredDataForMeasurementPointSeries>();
        foreach (var seriesElement in seriesElements)
        {
            var id = seriesElement.Element(ns + Identification)?.Value ?? string.Empty;
            var resolution = seriesElement.Element(ns + ObservationTimeSeriesPeriod)?.Element(ns + ResolutionDuration)?.Value;
            var startDateAndOrTimeDateTime = seriesElement.Element(ns + ObservationTimeSeriesPeriod)?.Element(ns + Start)?.Value ?? string.Empty;
            var endDateAndOrTimeDateTime = seriesElement.Element(ns + ObservationTimeSeriesPeriod)?.Element(ns + End)?.Value;
            var productNumber = seriesElement.Element(ns + IncludedProductCharacteristic)?.Element(ns + Identification)?.Value;
            var productUnitType = seriesElement.Element(ns + IncludedProductCharacteristic)?.Element(ns + UnitType)?.Value;
            var meteringPointType = seriesElement.Element(ns + DetailMeasurementMeteringPointCharacteristic)?.Element(ns + TypeOfMeteringPoint)?.Value;
            var meteringPointLocationId = seriesElement.Element(ns + MeteringPointDomainLocation)?.Element(ns + Identification)?.Value;

            var energyObservations = seriesElement
                .Descendants(ns + IntervalEnergyObservation)
                .Select(e => new EnergyObservation(
                    e.Element(ns + Position)?.Value,
                    e.Element(ns + EnergyQuantity)?.Value,
                    e.Element(ns + QuantityQuality)?.Value))
                .ToList();

            result.Add(new MeteredDataForMeasurementPointSeries(
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
