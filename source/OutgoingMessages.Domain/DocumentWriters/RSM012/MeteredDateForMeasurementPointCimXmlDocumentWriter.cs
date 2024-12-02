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
    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var wholesaleCalculationSeries in ParseFrom<MeteredDateForMeasurementPointMarketActivityRecord>(
                     marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);

            // tabbed content Series
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, wholesaleCalculationSeries.TransactionId).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("originalTransactionIDReference_Series.mRID", wholesaleCalculationSeries.OriginalTransactionIdReferenceId, writer).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "marketEvaluationPoint.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            writer.WriteValue(wholesaleCalculationSeries.MarketEvaluationPointNumber);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "marketEvaluationPoint.type", null, wholesaleCalculationSeries.MarketEvaluationPointType).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "registration_DateAndOrTime.dateTime", null, wholesaleCalculationSeries.RegistrationDateTime).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, wholesaleCalculationSeries.Product).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, wholesaleCalculationSeries.QuantityMeasureUnit).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Period", null).ConfigureAwait(false);

            // tabbed content Period
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "resolution", null, wholesaleCalculationSeries.Resolution).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);

            // tabbed content timeInterval
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "start", null, wholesaleCalculationSeries.StartedDateTime).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "end", null, wholesaleCalculationSeries.EndedDateTime).ConfigureAwait(false);

            // closing off timeInterval
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var point in wholesaleCalculationSeries.Points)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);

                // tabbed content Point
                await WriteElementAsync("position", point.Position.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);
                await WriteElementIfHasValueAsync("quantity", point.Quantity?.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);
                await WriteElementIfHasValueAsync("quality", point.Quality, writer).ConfigureAwait(false);

                // closing off Point
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            // closing off Period
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            // closing off series tag
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
