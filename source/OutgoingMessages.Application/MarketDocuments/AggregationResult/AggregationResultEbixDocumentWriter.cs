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
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;

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

    public override bool HandlesType(DocumentType documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
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

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Identification", null, timeSeries.TransactionId.ToString("N")).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Function", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
            await writer.WriteStringAsync("9").ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            // Begin ObservationTimeSeriesPeriod
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "ResolutionDuration", null, EbixCode.Of(Resolution.From(timeSeries.Resolution))).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Start", null, timeSeries.Period.StartToEbixString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "End", null, timeSeries.Period.EndToEbixString()).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End ObservationTimeSeriesPeriod

            if (timeSeries.BalanceResponsibleNumber != null)
            {
                // Begin BalanceResponsibleEnergyParty
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "BalanceResponsibleEnergyParty", null).ConfigureAwait(false);
                await WriteEbixSchemeCodeWithAttributesAsync("Identification", timeSeries.BalanceResponsibleNumber, writer).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End BalanceResponsibleEnergyParty
            }

            if (timeSeries.EnergySupplierNumber != null)
            {
                // Begin BalanceSupplierEnergyParty
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "BalanceSupplierEnergyParty", null).ConfigureAwait(false);
                await WriteEbixSchemeCodeWithAttributesAsync("Identification", timeSeries.EnergySupplierNumber, writer).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End BalanceSupplierEnergyParty
            }

            // Begin IncludedProductCharacteristic
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
            await writer.WriteStringAsync(GeneralValues.ProductCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await WriteEbixCodeWithAttributesAsync("UnitType", EbixCode.Of(MeasurementUnit.From(timeSeries.MeasureUnitType)), writer).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End IncludedProductCharacteristic

            // Begin DetailMeasurementMeteringPointCharacteristic
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
            await WriteEbixCodeWithAttributesAsync("TypeOfMeteringPoint", EbixCode.Of(MeteringPointType.From(timeSeries.MeteringPointType)), writer).ConfigureAwait(false);

            if (timeSeries.SettlementType != null)
            {
                await WriteEbixCodeWithAttributesAsync("SettlementMethod", EbixCode.Of(SettlementType.From(timeSeries.SettlementType)), writer).ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End DetailMeasurementMeteringPointCharacteristic

            // Begin MeteringGridAreaUsedDomainLocation
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringGridAreaUsedDomainLocation", null).ConfigureAwait(false);
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
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "EnergyQuantity", null, point.Quantity.ToString()!).ConfigureAwait(false);
                    await WriteEbixCodeWithAttributesAsync("QuantityQuality", EbixCode.Of(Quality.From(point.Quality)), writer).ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "QuantityMissing", null, "true").ConfigureAwait(false);
                }

                await writer.WriteEndElementAsync().ConfigureAwait(false);
                // End IntervalEnergyObservation
            }

            await WriteElementIfHasValueAsync("OriginalBusinessDocument", timeSeries.OriginalTransactionIdReference, writer).ConfigureAwait(false);

            // TODO XJOHO: We are currently not receiving Version from Wholesale - bug team-phoenix #78
            await WriteElementIfHasValueAsync("Version", "1", writer).ConfigureAwait(false);

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            // End PayloadEnergyTimeSeries
        }
    }
}
