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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;

public sealed class AcknowledgementXmlDocumentWriter(IMessageRecordParser parser)
    : CimXmlDocumentWriter(
          new DocumentDetails(
              "Acknowledgement_MarketDocument",
              "urn:ediel.org:general:acknowledgement:0:1 ack.xsd",
              "urn:ediel.org:general:acknowledgement:0:1",
              "cim",
              "ERR"),
          parser)
{
    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        // Parse the acknowledgement record (assumes a single payload) - is this right?
        var acknowledgement = ParseFrom<Acknowledgement>(marketActivityPayloads.Single());

        await WriteReceivedMarketDocumentElementsAsync(acknowledgement, writer).ConfigureAwait(false);
        await WriteReasonElementsAsync(acknowledgement.Reason, writer).ConfigureAwait(false);
        await WriteInErrorPeriodsAsync(acknowledgement.InErrorPeriod, writer).ConfigureAwait(false);
        await WriteSeriesElementsAsync(acknowledgement.Series, writer).ConfigureAwait(false);
        await WriteOriginalMktActivityRecordsAsync(acknowledgement.OriginalMktActivityRecord, writer).ConfigureAwait(false);
        await WriteRejectedTimeSeriesAsync(acknowledgement.RejectedTimeSeries, writer).ConfigureAwait(false);
    }

    private async Task WriteReceivedMarketDocumentElementsAsync(Acknowledgement acknowledgement, XmlWriter writer)
    {
        if (acknowledgement.ReceivedMarketDocumentCreatedDateTime.HasValue)
        {
            await WriteElementAsync(
                "received_MarketDocument.createdDateTime",
                acknowledgement.ReceivedMarketDocumentCreatedDateTime.Value
                    .ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                writer).ConfigureAwait(false);
        }

        await WriteElementIfHasValueAsync("received_MarketDocument.mRID", acknowledgement.ReceivedMarketDocumentTransactionId, writer).ConfigureAwait(false);
    }

    private async Task WriteReasonElementsAsync(IReadOnlyCollection<Reason>? reasons, XmlWriter writer)
    {
        if (reasons == null || reasons.Count == 0)
            return;

        foreach (var reason in reasons)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Reason", null).ConfigureAwait(false);
            await WriteElementAsync("code", reason.Code, writer).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(reason.Text))
            {
                await WriteElementAsync("text", reason.Text, writer).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteInErrorPeriodsAsync(IReadOnlyCollection<TimePeriod>? periods, XmlWriter writer)
    {
        if (periods == null || periods.Count == 0)
            return;

        foreach (var period in periods)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "InError_Period", null).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);
            await WriteElementAsync(
                "start",
                period.TimeInterval.Start.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                writer).ConfigureAwait(false);
            await WriteElementAsync(
                "end",
                period.TimeInterval.End.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
                writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await WriteReasonElementsAsync(period.Reason, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteSeriesElementsAsync(IReadOnlyCollection<Series>? seriesList, XmlWriter writer)
    {
        if (seriesList == null || seriesList.Count == 0)
            return;

        foreach (var series in seriesList)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await WriteElementAsync("mRID", series.MRID, writer).ConfigureAwait(false);
            await WriteReasonElementsAsync(series.Reason, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteOriginalMktActivityRecordsAsync(IReadOnlyCollection<MktActivityRecord>? records, XmlWriter writer)
    {
        if (records == null || records.Count == 0)
            return;

        foreach (var record in records)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Original_MktActivityRecord", null).ConfigureAwait(false);
            await WriteElementAsync("mRID", record.MRID, writer).ConfigureAwait(false);
            await WriteReasonElementsAsync(record.Reason, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteRejectedTimeSeriesAsync(IReadOnlyCollection<TimeSeries>? timeSeriesList, XmlWriter writer)
    {
        if (timeSeriesList == null || timeSeriesList.Count == 0)
            return;

        foreach (var ts in timeSeriesList)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Rejected_TimeSeries", null).ConfigureAwait(false);
            await WriteElementAsync("mRID", ts.MRID, writer).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("version", ts.Version, writer).ConfigureAwait(false);

            await WriteInErrorPeriodsAsync(ts.InErrorPeriod, writer).ConfigureAwait(false);

            await WriteReasonElementsAsync(ts.Reason, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
