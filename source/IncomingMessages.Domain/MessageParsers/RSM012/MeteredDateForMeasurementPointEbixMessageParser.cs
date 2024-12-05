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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;

public class MeteredDateForMeasurementPointEbixMessageParser(EbixSchemaProvider schemaProvider) : EbixMessageParserBase(schemaProvider)
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
    private const string MeteringPointType = "TypeOfMeteringPoint";
    private const string MeteringPointDomainLocation = "MeteringPointDomainLocation";
    private const string Position = "Position";
    private const string EnergyQuantity = "EnergyQuantity";
    private const string QuantityQuality = "QuantityQuality";
    private const string IntervalEnergyObservation = "IntervalEnergyObservation";

    public override IncomingDocumentType DocumentType => IncomingDocumentType.NotifyValidatedMeasureData;

    public override DocumentFormat DocumentFormat => DocumentFormat.Ebix;

    protected override string RootPayloadElementName => "DK_MeteredDataTimeSeries";

    protected override IReadOnlyCollection<IIncomingMessageSeries> ParseTransactions(XDocument document, XNamespace ns, string senderNumber)
    {
        var transactionElements = document.Descendants(ns + SeriesElementName);
        var result = new List<MeteredDataForMeasurementPointSeries>();
        foreach (var transactionElement in transactionElements)
        {
            var id = transactionElement.Element(ns + Identification)?.Value ?? string.Empty;
            var observationElement = transactionElement.Element(ns + ObservationTimeSeriesPeriod);
            var resolution = observationElement?.Element(ns + ResolutionDuration)?.Value;
            var startDateAndOrTimeDateTime = observationElement?.Element(ns + Start)?.Value ?? string.Empty;
            var endDateAndOrTimeDateTime = observationElement?.Element(ns + End)?.Value;
            var includedProductCharacteristicElement = transactionElement.Element(ns + IncludedProductCharacteristic);
            var productNumber = includedProductCharacteristicElement?.Element(ns + Identification)?.Value;
            var productUnitType = includedProductCharacteristicElement?.Element(ns + UnitType)?.Value;
            var meteringPointType = transactionElement.Element(ns + DetailMeasurementMeteringPointCharacteristic)?.Element(ns + MeteringPointType)?.Value;
            var meteringPointLocationId = transactionElement.Element(ns + MeteringPointDomainLocation)?.Element(ns + Identification)?.Value;

            var energyObservations = transactionElement
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
                null,
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
        return new IncomingMarketMessageParserResult(new MeteredDataForMeasurementPointMessageBase(
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
