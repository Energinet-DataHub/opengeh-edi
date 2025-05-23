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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM018;

public class MissingMeasurementEbixDocumentWriter(IMessageRecordParser parser)
    : EbixDocumentWriter(new DocumentDetails(
        type: "DK_NotifyMissingData",
        schemaLocation: string.Empty,
        xmlNamespace: "un:unece:260:data:EEM-DK_NotifyMissingData:v3",
        prefix: "ns0",
        typeCode: "D24"),
    parser)
{
    public override bool HandlesType(DocumentType documentType) => documentType == DocumentType.ReminderOfMissingMeasureData;

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);

        foreach (var missingMeasurementLog in ParseFrom<MissingMeasurementMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadMissingDataRequest", null)
                .ConfigureAwait(false);
            {
                await WriteTransactionIdAsync(writer, missingMeasurementLog).ConfigureAwait(false);

                await WriteMissingDateAsync(writer, missingMeasurementLog).ConfigureAwait(false);

                await WriteMeteringPointAsync(writer, missingMeasurementLog).ConfigureAwait(false);

                await WriteNumberOfRemindersAsync(writer).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteMissingDateAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "RequestPeriod",
            null,
            missingMeasurementLog.Date.ToString()).ConfigureAwait(false);
    }

    private async Task WriteTransactionIdAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "Identification",
            null,
            missingMeasurementLog.TransactionId.Value).ConfigureAwait(false);
    }

    private async Task WriteMeteringPointAsync(
        XmlWriter writer,
        MissingMeasurementMarketActivityRecord missingMeasurementLog)
    {
        await writer.WriteStartElementAsync(
            DocumentDetails.Prefix,
            "MeteringPointDomainLocation",
            null).ConfigureAwait(false);
        {
            await WriteMeteringPointIdAsync(
                missingMeasurementLog.MeteringPointId,
                writer)
                .ConfigureAwait(false);
        }

        await writer.WriteEndElementAsync().ConfigureAwait(false);
    }

    private async Task WriteNumberOfRemindersAsync(
        XmlWriter writer)
    {
        // Number of Reminders is always 0: https://app.zenhub.com/workspaces/mosaic-60a6105157304f00119be86e/issues/gh/energinet-datahub/team-mosaic/677
        await writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "NumberOfReminders",
            null,
            "0").ConfigureAwait(false);
    }
}
