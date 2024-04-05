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
using Energinet.DataHub.EDI.OutgoingMessages.Application.DocumentWriters.Ebix;
using Energinet.DataHub.EDI.OutgoingMessages.Application.DocumentWriters.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;

public class NotifyWholesaleServicesEbixDocumentWriter : EbixDocumentWriter
{
    public NotifyWholesaleServicesEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "DK_NotifyAggregatedWholesaleServices",
            string.Empty,
            "un:unece:260:data:EEM-DK_NotifyAggregatedWholesaleServices:v3",
            "ns0",
            "E31"),
            parser)
    {
    }

    public override bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return DocumentType.NotifyWholesaleServices == documentType;
    }

    protected override SettlementVersion? ExtractSettlementVersion(IReadOnlyCollection<string> marketActivityPayloads)
    {
        var payloads = ParseFrom<WholesaleCalculationMarketActivityRecord>(marketActivityPayloads);
        var settlementVersions = payloads
            .Where(ts => ts.SettlementVersion is not null)
            .Select(ts => ts.SettlementVersion)
            .Distinct()
            .ToList();

        return settlementVersions.Count switch
        {
            > 1 => throw new NotSupportedException("Multiple different settlementVersions in same message is not supported in ebIX"),
            1 => settlementVersions.First()!,
            _ => null,
        };
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var series in ParseFrom<WholesaleCalculationMarketActivityRecord>(marketActivityPayloads))
        {
            // Begin PayloadEnergyTimeSeries
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "PayloadEnergyTimeSeries", null).ConfigureAwait(false);
            {
                // <Identification />
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Identification", null, series.TransactionId.ToString("N")).ConfigureAwait(false);

                // <Function />
                await WriteCodeWithCodeListReferenceAttributesAsync("Function", "9", writer).ConfigureAwait(false);

                // <Currency /> -- Currency is hardcoded to listAgencyIdentifier = ebIX list (260), without country code
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Currency", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
                await writer.WriteStringAsync(EbixCode.Of(series.Currency)).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);

                // Begin <ObservationTimeSeriesPeriod>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ObservationTimeSeriesPeriod", null).ConfigureAwait(false);
                {
                    // <ResolutionDuration />
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "ResolutionDuration", null, EbixCode.Of(series.Resolution)).ConfigureAwait(false);

                    // <Start />
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Start", null, series.Period.StartToEbixString()).ConfigureAwait(false);

                    // <End />
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "End", null, series.Period.EndToEbixString()).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </ObservationTimeSeriesPeriod>

                // Begin <BalanceSupplierEnergyParty>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "BalanceSupplierEnergyParty", null).ConfigureAwait(false);
                {
                    // <Identification />
                    await WriteGlnOrEicCodeWithAttributesAsync("Identification", series.EnergySupplier.Value, writer).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </BalanceSupplierEnergyParty>

                // Begin <IncludedProductCharacteristic>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);
                {
                    // <Identification />, product attribute "listAgencyIdentifier" must be hardcoded to 9
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
                    await writer.WriteStringAsync(ProductType.Tariff.Code).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

#pragma warning disable CS0618 // Type or member is obsolete
                    // <UnitType />
                    await WriteCodeWithCodeListReferenceAttributesAsync("UnitType", EbixCode.Of(series.QuantityUnit ?? series.QuantityMeasureUnit), writer).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </IncludedProductCharacteristic>

#pragma warning disable CS0618 // Type or member is obsolete
                if (series.MeteringPointType != null || series.SettlementType != null || series.SettlementMethod != null)
                {
                    // Begin DetailMeasurementMeteringPointCharacteristic
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "DetailMeasurementMeteringPointCharacteristic", null).ConfigureAwait(false);
                    if (series.MeteringPointType != null)
                        await WriteCodeWithCodeListReferenceAttributesAsync("TypeOfMeteringPoint", series.MeteringPointType.Code, writer).ConfigureAwait(false);

                    if (series.SettlementType != null || series.SettlementMethod != null)
                        await WriteCodeWithCodeListReferenceAttributesAsync("SettlementMethod", series.SettlementType?.Code ?? series.SettlementMethod!.Code, writer).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                }
#pragma warning restore CS0618 // Type or member is obsolete

                // End DetailMeasurementMeteringPointCharacteristic

                // Begin <MeteringGridAreaUsedDomainLocation>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringGridAreaUsedDomainLocation", null).ConfigureAwait(false);
                {
                    // <Identification /> (GridAreaCode) attributes "schemeAgencyIdentifier" must be hardcoded to 260, and "schemeIdentifier" to DK
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeIdentifier", null, "DK").ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "260").ConfigureAwait(false);
                    await writer.WriteStringAsync(series.GridAreaCode).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </MeteringGridAreaUsedDomainLocation>

                if (series.Points != null)
                {
                    foreach (var point in series.Points)
                    {
                        // Begin <IntervalEnergyObservation>
                        await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null)
                            .ConfigureAwait(false);
                        {
                            // <Position />
                            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo))
                                .ConfigureAwait(false);

                            // <EnergyQuantity />
                            if (point.Quantity != null)
                            {
                                await writer.WriteElementStringAsync(
                                        DocumentDetails.Prefix,
                                        "EnergyQuantity",
                                        null,
                                        point.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo))
                                    .ConfigureAwait(false);
                            }

                            // <EnergyPrice />
                            if (point.Price != null)
                            {
                                await writer.WriteElementStringAsync(
                                        DocumentDetails.Prefix,
                                        "EnergyPrice",
                                        null,
                                        point.Price.Value.ToString(NumberFormatInfo.InvariantInfo))
                                    .ConfigureAwait(false);
                            }

                            // <QuantityQuality />
                            if (point.QuantityQuality != null && EbixCode.WholesaleServicesOf(point.QuantityQuality.Value) is not null)
                            {
                                await WriteCodeWithCodeListReferenceAttributesAsync(
                                        "QuantityQuality",
                                        EbixCode.WholesaleServicesOf(point.QuantityQuality.Value)!,
                                        writer)
                                    .ConfigureAwait(false);
                            }

                            // <EnergySum />
                            await writer.WriteElementStringAsync(
                                    DocumentDetails.Prefix,
                                    "EnergySum",
                                    null,
                                    point.Amount?.ToString(NumberFormatInfo.InvariantInfo) ?? "0")
                                .ConfigureAwait(false);

                            await writer.WriteEndElementAsync().ConfigureAwait(false);
                        } // End </IntervalEnergyObservation>
                    }
                }

                // <ChargeType />
                await WriteCodeWithCodeListReferenceAttributesAsync("ChargeType", EbixCode.Of(series.ChargeType), writer).ConfigureAwait(false);

                // <PartyChargeTypeID />
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "PartyChargeTypeID", null, series.ChargeCode).ConfigureAwait(false);

                // <OriginalBusinessDocument />
                await WriteElementIfHasValueAsync(
                        "OriginalBusinessDocument",
                        series.OriginalTransactionIdReference,
                        writer)
                    .ConfigureAwait(false);

                // Begin <ChargeTypeOwnerEnergyParty>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ChargeTypeOwnerEnergyParty", null).ConfigureAwait(false);
                {
                    // <Identification />
                    await WriteGlnOrEicCodeWithAttributesAsync("Identification", series.ChargeOwner.Value, writer).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </ChargeTypeOwnerEnergyParty>

                // <Version />
                await WriteElementIfHasValueAsync("Version", series.CalculationVersion.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            } // End PayloadEnergyTimeSeries
        }
    }
}
