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
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Xml;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Infrastructure.OutgoingMessages.Common.Xml;

namespace Messaging.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataMessageWriter : MessageWriter
{
    public NotifyAggregatedMeasureDataMessageWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "NotifyAggregatedMeasureData_MarketDocument",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1 urn-ediel-org-measure-notifyaggregatedmeasuredata-0-1.xsd",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1",
            "cim",
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
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, timeSeries.Id.ToString()).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "marketEvaluationPoint.type", null, timeSeries.MeteringPointType).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
            await writer.WriteStringAsync(timeSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, timeSeries.MeasureUnitType).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Period", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "resolution", null, timeSeries.Resolution).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "start", null, timeSeries.StartTime).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "end", null, timeSeries.EndTime).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var point in timeSeries.Point)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity", null, point.Quantity.ToString()!).ConfigureAwait(false);
                }

                if (point.Quality is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quality", null, point.Quality).ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
