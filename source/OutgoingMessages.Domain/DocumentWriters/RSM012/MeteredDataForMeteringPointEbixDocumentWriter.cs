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
    private const string PayloadEnergyTimeSeriesName = "PayloadEnergyTimeSeries";
    private const string FunctionName = "Function";
    private const string IdentificationName = "Identification";
    private const string ListAgencyIdentifierName = "listAgencyIdentifier";
    private const string ObservationTimeSeriesPeriodName = "ObservationTimeSeriesPeriod";
    private const string ResolutionDurationName = "ResolutionDuration";
    private const string IntervalEnergyObservationName = "IntervalEnergyObservation";
    private const string PositionName = "Position";
    private const string EnergyQuantityName = "EnergyQuantity";
    private const string QuantityQualityName = "QuantityQuality";
    private const string QuantityMissingName = "QuantityMissing";
    private const string TrueValue = "true";
    private const string OriginalBusinessDocumentName = "OriginalBusinessDocument";
    private const string MeteringPointDomainLocationName = "MeteringPointDomainLocation";
    private const string DetailMeasurementMeteringPointCharacteristicName = "DetailMeasurementMeteringPointCharacteristic";
    private const string TypeOfMeteringPointName = "TypeOfMeteringPoint";
    private const string IncludedProductCharacteristicName = "IncludedProductCharacteristic";
    private const string UnitTypeName = "UnitType";
    private const string? Value260 = "260";
    private const string? Value9 = "9";
    private const string EndName = "End";
    private const string StartName = "Start";
    private const string? Value6 = "6";

    private static readonly NumberFormatInfo _numberFormatInfo = NumberFormatInfo.InvariantInfo;

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

        foreach (var forwardMeteredDataRecordPayload in marketActivityPayloads)
        {
            var forwardMeteredDataRecord = ParseFrom<MeteredDataForMeteringPointMarketActivityRecord>(forwardMeteredDataRecordPayload);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, PayloadEnergyTimeSeriesName, null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                IdentificationName,
                null,
                forwardMeteredDataRecord.TransactionId.Value).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, FunctionName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value6).ConfigureAwait(false);
            await writer.WriteStringAsync(Value9).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, ObservationTimeSeriesPeriodName, null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                ResolutionDurationName,
                null,
                forwardMeteredDataRecord.Resolution.Code)
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                StartName,
                null,
                forwardMeteredDataRecord.StartedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                EndName,
                null,
                forwardMeteredDataRecord.EndedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, IncludedProductCharacteristicName, null).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, IdentificationName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value9).ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.Product).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, UnitTypeName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value260).ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.QuantityMeasureUnit.Code).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, DetailMeasurementMeteringPointCharacteristicName, null).ConfigureAwait(false);
            await WriteCodeWithCodeListReferenceAttributesAsync(
                    TypeOfMeteringPointName,
                    EbixCode.Of(forwardMeteredDataRecord.MarketEvaluationPointType),
                    writer)
                .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, MeteringPointDomainLocationName, null).ConfigureAwait(false);
            await WriteGlnOrEicCodeWithAttributesAsync(IdentificationName, forwardMeteredDataRecord.MarketEvaluationPointNumber, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var energyObservation in forwardMeteredDataRecord.EnergyObservations)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, IntervalEnergyObservationName, null).ConfigureAwait(false);

                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    PositionName,
                    null,
                    energyObservation.Position.ToString())
                .ConfigureAwait(false);

                if (energyObservation.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        EnergyQuantityName,
                        null,
                        energyObservation.Quantity.Value.ToString(_numberFormatInfo))
                    .ConfigureAwait(false);

                    if (energyObservation.Quality is not null)
                    {
                        var quality = EbixCode.Of(energyObservation.Quality);
                        if (quality is not null)
                        {
                            await WriteCodeWithCodeListReferenceAttributesAsync(QuantityQualityName, quality, writer)
                                .ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        QuantityMissingName,
                        null,
                        TrueValue)
                    .ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (forwardMeteredDataRecord.OriginalTransactionIdReferenceId is not null)
            {
                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    OriginalBusinessDocumentName,
                    null,
                    forwardMeteredDataRecord.OriginalTransactionIdReferenceId.Value)
                .ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<FileStorageFile> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var payloadFile in marketActivityPayloads)
        {
            // var payloadString = await payloadFile.ReadAsStringAsync().ConfigureAwait(false);
            var forwardMeteredDataRecord = await ParseFromAsync<MeteredDataForMeteringPointMarketActivityRecord>(
                    payloadFile,
                    CancellationToken.None)
                .ConfigureAwait(false);
            // Dispose the payload after reading, to avoid holding all files in a bundle in memory
            // payloadString = null; // "Dispose" the string by removing the reference to it
            payloadFile.Dispose();
            GC.Collect();

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, PayloadEnergyTimeSeriesName, null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                IdentificationName,
                null,
                forwardMeteredDataRecord.TransactionId.Value).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, FunctionName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value6).ConfigureAwait(false);
            await writer.WriteStringAsync(Value9).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, ObservationTimeSeriesPeriodName, null)
                .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                ResolutionDurationName,
                null,
                forwardMeteredDataRecord.Resolution.Code)
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                StartName,
                null,
                forwardMeteredDataRecord.StartedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                EndName,
                null,
                forwardMeteredDataRecord.EndedDateTime.ToString())
            .ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, IncludedProductCharacteristicName, null).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, IdentificationName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value9).ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.Product).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, UnitTypeName, null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, ListAgencyIdentifierName, null, Value260).ConfigureAwait(false);
            await writer.WriteStringAsync(forwardMeteredDataRecord.QuantityMeasureUnit.Code).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, DetailMeasurementMeteringPointCharacteristicName, null).ConfigureAwait(false);
            await WriteCodeWithCodeListReferenceAttributesAsync(
                TypeOfMeteringPointName,
                EbixCode.Of(forwardMeteredDataRecord.MarketEvaluationPointType),
                writer)
            .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, MeteringPointDomainLocationName, null).ConfigureAwait(false);
            await WriteGlnOrEicCodeWithAttributesAsync(IdentificationName, forwardMeteredDataRecord.MarketEvaluationPointNumber, writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            foreach (var energyObservation in forwardMeteredDataRecord.EnergyObservations)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, IntervalEnergyObservationName, null).ConfigureAwait(false);

                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    PositionName,
                    null,
                    energyObservation.Position.ToString())
                .ConfigureAwait(false);

                if (energyObservation.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        EnergyQuantityName,
                        null,
                        energyObservation.Quantity.Value.ToString(_numberFormatInfo))
                    .ConfigureAwait(false);

                    if (energyObservation.Quality is not null)
                    {
                        var quality = EbixCode.Of(energyObservation.Quality);
                        if (quality is not null)
                        {
                            await WriteCodeWithCodeListReferenceAttributesAsync(QuantityQualityName, quality, writer)
                                .ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await writer.WriteElementStringAsync(
                        DocumentDetails.Prefix,
                        QuantityMissingName,
                        null,
                        TrueValue)
                    .ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (forwardMeteredDataRecord.OriginalTransactionIdReferenceId is not null)
            {
                await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    OriginalBusinessDocumentName,
                    null,
                    forwardMeteredDataRecord.OriginalTransactionIdReferenceId.Value)
                .ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);
        }
    }
}
