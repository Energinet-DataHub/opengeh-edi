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

using System.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Xml;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM018;

public class MissingMeasurementXmlDocumentWriter(
    IMessageRecordParser parser)
    : CimXmlDocumentWriter(
        new DocumentDetails(
            "ReminderOfMissingMeasureData_MarketDocument",
            "urn:ediel.org:measure:reminderofmissingmeasuredata:0:1 urn-ediel-org-measure-reminderofmissingmeasuredata-0-1.xsd",
            "urn:ediel.org:measure:reminderofmissingmeasuredata:0:1",
            "cim",
            "D24"),
        parser)
{
    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        foreach (var missingMeasurementLog in ParseFrom<MissingMeasurementMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            {
                await WriteTransactionIdAsync(writer, missingMeasurementLog).ConfigureAwait(false);

                await WriteMissingDateAsync(writer, missingMeasurementLog).ConfigureAwait(false);

                await WriteMeteringPointAsync(writer, missingMeasurementLog).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteTransactionIdAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "mRID",
            null,
            missingMeasurementLog.TransactionId.Value).ConfigureAwait(false);
    }

    private async Task WriteMissingDateAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "request_DateAndOrTime.dateTime",
            null,
            missingMeasurementLog.Date.ToString()).ConfigureAwait(false);
    }

    private async Task WriteMeteringPointAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteStartElementAsync(
            DocumentDetails.Prefix,
            "MarketEvaluationPoint",
            null).ConfigureAwait(false);
        {
            await writer.WriteStartElementAsync(
                DocumentDetails.Prefix,
                "mRID",
                null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "A10").ConfigureAwait(false);
            await writer.WriteStringAsync(missingMeasurementLog.MeteringPointId.Value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }
}
