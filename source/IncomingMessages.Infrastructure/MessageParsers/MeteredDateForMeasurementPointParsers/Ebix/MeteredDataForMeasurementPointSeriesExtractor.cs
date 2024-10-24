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

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.MeteredDateForMeasurementPointParsers.Ebix;

public class MeteredDataForMeasurementPointSeriesExtractor
{
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

    protected internal static IEnumerable<MeteredDataForMeasurementPointSeries> ParseSeries(
        XDocument document,
        XNamespace ns,
        string senderNumber,
        string seriesElementName)
    {
        var seriesElements = document.Descendants(ns + seriesElementName);

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

            yield return new MeteredDataForMeasurementPointSeries(
                id,
                resolution,
                startDateAndOrTimeDateTime,
                endDateAndOrTimeDateTime,
                productNumber,
                productUnitType,
                meteringPointType,
                meteringPointLocationId,
                senderNumber,
                energyObservations);
        }
    }
}
