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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using Application.IncomingMessages;
using Application.OutgoingMessages.Common;
using Application.OutgoingMessages.Common.Xml;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.Common.Xml;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Infrastructure.OutgoingMessages.AggregationResult;

public class AggregationResultEbixDocumentWriter : EbixDocumentWriter
{
    public AggregationResultEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "DK_AggregatedMeteredDataTimeSeries",
            string.Empty,
            "un:unece:260:data:EEM-DK_AggregatedMeteredDataTimeSeries:v3",
            "ns0",
            "E31"),
            parser,
            null)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var timeSeries in ParseFrom<TimeSeries>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadEnergyTimeSeries", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Identification", null, timeSeries.TransactionId.ToString()).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Function", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync("9").ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "ResolutionDuration", null, EbixCode.Of(Resolution.From(timeSeries.Resolution))).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Start", null, timeSeries.Period.StartToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "End", null, timeSeries.Period.EndToString()).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false); // End ObservationTimeSeriesPeriod

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
            await writer.WriteStringAsync(GeneralValues.ProductCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "UnitType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(MeasurementUnit.From(timeSeries.MeasureUnitType))).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false); // End IncludedProductCharacteristic

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "TypeOfMeteringPoint", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(EbixCode.Of(MeteringPointType.From(timeSeries.MeteringPointType))).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false); // End DetailMeasurementMeteringPointCharacteristic

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringGridAreaUsedDomainLocation", null).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "schemeIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(timeSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false); // End MeteringGridAreaUsedDomainLocation

            foreach (var point in timeSeries.Point)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "EnergyQuantity", null, point.Quantity.ToString()!).ConfigureAwait(false);
                }

                await WriteQualityIfRequiredAsync(writer, point).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false); // End IntervalEnergyObservation
            }

            /*
            if (timeSeries.OriginalTransactionIdReference is not null)
            {
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "OriginalBusinessDocument", null, timeSeries.OriginalTransactionIdReference).ConfigureAwait(false);
            }
            */
            await WriteElementIfHasValueAsync("OriginalBusinessDocument", timeSeries.OriginalTransactionIdReference, writer).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("Version", timeSeries.SettlementVersion, writer).ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false); // End PayloadEnergyTimeSeries
        }
    }

    private Task WriteQualityIfRequiredAsync(XmlWriter writer, Point point)
    {
        if (point.Quality is null)
            return Task.CompletedTask;

        if (Quality.From(point.Quality) == Quality.Measured)
            return Task.CompletedTask;

        return writer.WriteElementStringAsync(DocumentDetails.Prefix, "QuantityQuality", null, EbixCode.Of(Quality.From(point.Quality)));
    }
}
