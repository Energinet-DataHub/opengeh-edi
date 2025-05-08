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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.Ebix;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

public class MeteredDataForMeteringPointEbixDocumentWriter(IMessageRecordParser parser)
    : EbixDocumentWriter(new DocumentDetails(
            type: "DK_MeteredDataTimeSeries",
            schemaLocation: string.Empty,
            xmlNamespace: "un:unece:260:data:EEM-DK_MeteredDataTimeSeries:v3",
            prefix: "ns0",
            typeCode: "E66"),
        parser)
{
    public override bool HandlesType(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyValidatedMeasureData;
    }

    protected override async Task WriteMarketActivityRecordsAsync(
        IReadOnlyCollection<string> marketActivityPayloads,
        XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var forwardMeteredDataRecord in ParseFrom<MeteredDataForMeteringPointMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadEnergyTimeSeries", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "Identification",
                null,
                forwardMeteredDataRecord.TransactionId.Value).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Function", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync("9").ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "ResolutionDuration",
                null,
                forwardMeteredDataRecord.Resolution.Code)
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "Start",
                null,
                forwardMeteredDataRecord.Period.StartToEbixString())
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "End",
                null,
                forwardMeteredDataRecord.Period.EndToEbixString())
            .ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.Product).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "UnitType", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.MeasurementUnit.Code).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
            await WriteCodeWithCodeListReferenceAttributesAsync(
                "TypeOfMeteringPoint",
                EbixCode.Of(forwardMeteredDataRecord.MeteringPointType),
                writer)
            .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringPointDomainLocation", null).ConfigureAwait(false);
            await WriteGlnOrEicCodeWithAttributesAsync("Identification", forwardMeteredDataRecord.MeteringPointId, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var energyObservation in forwardMeteredDataRecord.Measurements)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null).ConfigureAwait(false);

                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Position",
                    null,
                    energyObservation.Position.ToString())
                .ConfigureAwait(false);

                if (energyObservation.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        "EnergyQuantity",
                        null,
                        energyObservation.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo))
                    .ConfigureAwait(false);

                    if (energyObservation.Quality is not null)
                    {
                        var quality = EbixCode.Of(energyObservation.Quality);
                        if (quality is not null)
                        {
                            await WriteCodeWithCodeListReferenceAttributesAsync("QuantityQuality", quality, writer)
                                .ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        "QuantityMissing",
                        null,
                        "true")
                    .ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (forwardMeteredDataRecord.OriginalTransactionIdReference is not null)
            {
                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "OriginalBusinessDocument",
                    null,
                    forwardMeteredDataRecord.OriginalTransactionIdReference.Value)
                .ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
