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

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Xml;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

public class MeteredDateForMeasurementPointCimXmlDocumentWriter(
    IMessageRecordParser parser)
    : CimXmlDocumentWriter(
        new DocumentDetails(
            "NotifyValidatedMeasureData_MarketDocument",
            "urn:ediel.org:measure:notifyvalidatedmeasuredata:0:1 urn-ediel-org-measure-notifyvalidatedmeasuredata-0-1.xsd",
            "urn:ediel.org:measure:notifyvalidatedmeasuredata:0:1",
            "cim",
            "E66"),
        parser)
{
    protected override async Task WriteMarketActivityRecordsAsync(
        IReadOnlyCollection<string> marketActivityPayloads,
        XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);
        XNamespace @namespace = "urn:ediel.org:measure:notifyvalidatedmeasuredata:0:1";
        foreach (var wholesaleCalculationSeries in ParseFrom<MeteredDateForMeasurementPointMarketActivityRecord>(
                     marketActivityPayloads))
        {
            var seriesElement = new XElement(
                @namespace + "Series",
                new XElement(@namespace + "mRID", wholesaleCalculationSeries.TransactionId),
                new XElement(
                    @namespace + "originalTransactionIDReference_Series.mRID",
                    wholesaleCalculationSeries.OriginalTransactionIdReferenceId),
                new XElement(
                    @namespace + "marketEvaluationPoint.mRID",
                    new XAttribute("codingScheme", "A10"),
                    wholesaleCalculationSeries.MarketEvaluationPointNumber),
                new XElement(@namespace + "marketEvaluationPoint.type", wholesaleCalculationSeries.MarketEvaluationPointType),
                new XElement(@namespace + "registration_DateAndOrTime.dateTime", wholesaleCalculationSeries.RegistrationDateTime),
                new XElement(@namespace + "product", wholesaleCalculationSeries.Product),
                new XElement(@namespace + "quantity_Measure_Unit.name", wholesaleCalculationSeries.QuantityMeasureUnit),
                new XElement(
                    @namespace + "Period",
                    new XElement(@namespace + "resolution", wholesaleCalculationSeries.Resolution),
                    new XElement(
                        @namespace + "timeInterval",
                        new XElement(@namespace + "start", wholesaleCalculationSeries.StartedDateTime),
                        new XElement(@namespace + "end", wholesaleCalculationSeries.EndedDateTime)),
                    wholesaleCalculationSeries.Points.Select(x => CreatePointElement(x, @namespace))));

            await seriesElement.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private XElement CreatePointElement(PointActivityRecord point, XNamespace @namespace)
    {
        var pointElement = new XElement(
            @namespace + "Point",
            new XElement(@namespace + "position", point.Position.ToString(NumberFormatInfo.InvariantInfo)));

        if (point.Quantity != null)
        {
            pointElement.Add(new XElement(@namespace + "quantity", point.Quantity?.ToString(NumberFormatInfo.InvariantInfo)));
        }

        if (point.Quality != null)
        {
            pointElement.Add(new XElement(@namespace + "quality", point.Quality?.ToString(NumberFormatInfo.InvariantInfo)));
        }

        return pointElement;
    }
}
