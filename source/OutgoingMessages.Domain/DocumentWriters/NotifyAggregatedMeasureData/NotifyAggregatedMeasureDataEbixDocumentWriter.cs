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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataEbixDocumentWriter : EbixDocumentWriter
{
    public NotifyAggregatedMeasureDataEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "DK_AggregatedMeteredDataTimeSeries",
            string.Empty,
            "un:unece:260:data:EEM-DK_AggregatedMeteredDataTimeSeries:v3",
            "ns0",
            "E31"),
            parser)
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.NotifyAggregatedMeasureData == documentType;
    }

    protected override SettlementVersion? ExtractSettlementVersion(IReadOnlyCollection<string> marketActivityPayloads)
    {
        var payloads = ParseFrom<TimeSeriesMarketActivityRecord>(marketActivityPayloads);
        var settlementVersions = payloads.Where(ts => ts.SettlementVersion is not null).Select(ts => ts.SettlementVersion)?.Distinct().ToList();
        if (settlementVersions?.Count > 1)
        {
            throw new NotSupportedException("Multiple different settlementVersions in same message is not supported in ebIX");
        }
        else if (settlementVersions?.Count == 1)
        {
            return SettlementVersion.FromName(settlementVersions.First()!);
        }
        else
        {
            return null;
        }
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var timeSeries in ParseFrom<TimeSeriesMarketActivityRecord>(marketActivityPayloads))
        {
            // Begin PayloadEnergyTimeSeries
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadEnergyTimeSeries", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "Identification",
                    null,
                    timeSeries.TransactionId.Value)
                .ConfigureAwait(false);

            await WriteCodeWithCodeListReferenceAttributesAsync("Function", "9", writer).ConfigureAwait(false);

            // Begin ObservationTimeSeriesPeriod
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "ResolutionDuration", null, EbixCode.Of(Resolution.FromName(timeSeries.Resolution))).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Start", null, timeSeries.Period.StartToEbixString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "End", null, timeSeries.Period.EndToEbixString()).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End ObservationTimeSeriesPeriod

            if (timeSeries.BalanceResponsibleNumber != null)
            {
                // Begin BalanceResponsibleEnergyParty
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "BalanceResponsibleEnergyParty", null).ConfigureAwait(false);
                await WriteGlnOrEicCodeWithAttributesAsync("Identification", timeSeries.BalanceResponsibleNumber, writer).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End BalanceResponsibleEnergyParty
            }

            if (timeSeries.EnergySupplierNumber != null)
            {
                // Begin BalanceSupplierEnergyParty
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "BalanceSupplierEnergyParty", null).ConfigureAwait(false);
                await WriteGlnOrEicCodeWithAttributesAsync("Identification", timeSeries.EnergySupplierNumber, writer).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End BalanceSupplierEnergyParty
            }

            // Begin IncludedProductCharacteristic
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);

            // Product attribute "listAgencyIdentifier" must be hardcoded to 9
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
            await writer.WriteStringAsync(ProductType.EnergyActive.Code).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            var outdatedValue = string.Equals(timeSeries.MeasureUnitType, "Kwh", StringComparison.InvariantCultureIgnoreCase);
            var ebixCode = outdatedValue ? EbixCode.Of(MeasurementUnit.KilowattHour) : EbixCode.Of(MeasurementUnit.FromName(timeSeries.MeasureUnitType));
            await WriteCodeWithCodeListReferenceAttributesAsync(
                    "UnitType",
                    ebixCode,
                    writer)
                .ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End IncludedProductCharacteristic

            // Begin DetailMeasurementMeteringPointCharacteristic
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
            await WriteCodeWithCodeListReferenceAttributesAsync("TypeOfMeteringPoint", EbixCode.Of(MeteringPointType.FromName(timeSeries.MeteringPointType)), writer).ConfigureAwait(false);

            if (timeSeries.SettlementMethod != null)
            {
                    await WriteCodeWithCodeListReferenceAttributesAsync(
                            "SettlementMethod",
                            EbixCode.Of(SettlementMethod.FromName(timeSeries.SettlementMethod!)),
                            writer)
                        .ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End DetailMeasurementMeteringPointCharacteristic

            // Begin MeteringGridAreaUsedDomainLocation
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringGridAreaUsedDomainLocation", null).ConfigureAwait(false);

            // // <MeteringGridAreaUsedDomainLocation.Identification /> (GridAreaCode) attributes "schemeAgencyIdentifier" must be hardcoded to 260, and "schemeIdentifier" to DK
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "schemeIdentifier", null, "DK").ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "260").ConfigureAwait(false);
            await writer.WriteStringAsync(timeSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End MeteringGridAreaUsedDomainLocation

            foreach (var point in timeSeries.Point)
            {
                // Begin IntervalEnergyObservation
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null
                    && EbixCode.ForEnergyResultOf(point.QuantityQuality) is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "EnergyQuantity", null, point.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                    await WriteCodeWithCodeListReferenceAttributesAsync(
                            "QuantityQuality",
                            EbixCode.ForEnergyResultOf(point.QuantityQuality)!,
                            writer)
                        .ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "QuantityMissing", null, "true").ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End IntervalEnergyObservation
            }

            await WriteElementIfHasValueAsync(
                    "OriginalBusinessDocument",
                    timeSeries.OriginalTransactionIdReference?.Value,
                    writer)
                .ConfigureAwait(false);

            await WriteElementIfHasValueAsync("Version", timeSeries.CalculationResultVersion.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End PayloadEnergyTimeSeries
        }
    }
}
