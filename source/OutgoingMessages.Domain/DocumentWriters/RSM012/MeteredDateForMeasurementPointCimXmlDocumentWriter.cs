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
        foreach (var activityRecord in ParseFrom<MeteredDateForMeasurementPointMarketActivityRecord>(marketActivityPayloads))
        {
            var seriesElement = new XElement(@namespace + "Series");
            seriesElement.Add(new XElement(@namespace + "mRID", activityRecord.TransactionId.Value));

            if (activityRecord.OriginalTransactionIdReferenceId is not null)
            {
                seriesElement.Add(
                    new XElement(
                    @namespace + "originalTransactionIDReference_Series.mRID",
                    activityRecord.OriginalTransactionIdReferenceId?.Value));
            }

            seriesElement.Add(
                new XElement(
                    @namespace + "marketEvaluationPoint.mRID",
                    new XAttribute("codingScheme", "A10"),
                    activityRecord.MarketEvaluationPointNumber));

            seriesElement.Add(
                new XElement(@namespace + "marketEvaluationPoint.type", activityRecord.MarketEvaluationPointType));

            if (activityRecord.RegistrationDateTime is not null)
            {
                seriesElement.Add(
                    new XElement(
                        @namespace + "registration_DateAndOrTime.dateTime",
                        activityRecord.RegistrationDateTime));
            }

            if (activityRecord.Product is not null)
            {
                seriesElement.Add(new XElement(@namespace + "product", activityRecord.Product));
            }

            seriesElement.Add(
                new XElement(@namespace + "quantity_Measure_Unit.name", activityRecord.QuantityMeasureUnit.Code));

            seriesElement.Add(
                new XElement(
                    @namespace + "Period",
                    new XElement(@namespace + "resolution", activityRecord.Resolution.Code),
                    new XElement(
                        @namespace + "timeInterval",
                        new XElement(@namespace + "start", activityRecord.StartedDateTime.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)),
                        new XElement(@namespace + "end", activityRecord.EndedDateTime.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))),
                    activityRecord.EnergyObservations.Select(x => CreatePointElement(x, @namespace))));

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
