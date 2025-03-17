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
        var rejectedForwardMeteredDataRecords = ParseFrom<RejectedForwardMeteredDataRecord>(marketActivityPayloads);

        foreach (var rejectedForwardMeteredDataRecord in rejectedForwardMeteredDataRecords)
        {
            await WriteReasonElementsAsync(rejectedForwardMeteredDataRecord.RejectReasons, writer).ConfigureAwait(false);
        }
    }

    private async Task WriteReasonElementsAsync(IReadOnlyCollection<RejectReason>? reasons, XmlWriter writer)
    {
        if (reasons == null || reasons.Count == 0)
            return;

        foreach (var reason in reasons)
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Reason", null).ConfigureAwait(false);
            await WriteElementAsync("code", reason.ErrorCode, writer).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(reason.ErrorMessage))
            {
                await WriteElementAsync("text", reason.ErrorMessage, writer).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
