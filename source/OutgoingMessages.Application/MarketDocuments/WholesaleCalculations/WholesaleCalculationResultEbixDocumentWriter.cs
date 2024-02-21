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

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.WholesaleCalculations;

public class WholesaleCalculationResultEbixDocumentWriter : EbixDocumentWriter
{
    public WholesaleCalculationResultEbixDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "DK_NotifyAggregatedWholesaleServices",
            string.Empty,
            "un:unece:260:data:EEM-DK_NotifyAggregatedWholesaleServices",
            "ns0",
            "E31"),
            parser,
            null)
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
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Function", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "6").ConfigureAwait(false);
                await writer.WriteStringAsync("9").ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);

                // <Currency />
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
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
                    await writer.WriteStringAsync(series.EnergySupplier.Value).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </BalanceSupplierEnergyParty>

                // Begin <IncludedProductCharacteristic>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IncludedProductCharacteristic", null).ConfigureAwait(false);
                {
                    // <Identification />
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "9").ConfigureAwait(false);
                    await writer.WriteStringAsync(DocumentGeneralValues.ProductCodeA).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    // <UnitType />
                    await WriteEbixCodeWithAttributesAsync("UnitType", EbixCode.Of(series.QuantityUnit), writer).ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </IncludedProductCharacteristic>

                // Begin <MeteringGridAreaUsedDomainLocation>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "MeteringGridAreaUsedDomainLocation", null).ConfigureAwait(false);
                {
                    // <Identification />
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeIdentifier", null, "DK").ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "260").ConfigureAwait(false);
                    await writer.WriteStringAsync(series.GridAreaCode).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </MeteringGridAreaUsedDomainLocation>

                // Begin <IntervalEnergyObservation>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "IntervalEnergyObservation", null).ConfigureAwait(false);
                {
                    // <Position />
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "Position", null, "1").ConfigureAwait(false);

                    // <EnergySum />
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "EnergySum", null, series.Quantity?.ToString(NumberFormatInfo.InvariantInfo) ?? "0").ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </IntervalEnergyObservation>

                // <ChargeType />
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ChargeType", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "listAgencyIdentifier", null, "260").ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "listIdentifier", null, "DK").ConfigureAwait(false);
                await writer.WriteStringAsync(EbixCode.Of(series.ChargeType)).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);

                // <PartyChargeTypeID />
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "PartyChargeTypeID", null, series.ChargeCode).ConfigureAwait(false);

                // Begin <ChargeTypeOwnerEnergyParty>
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "ChargeTypeOwnerEnergyParty", null).ConfigureAwait(false);
                {
                    // <Identification />
                    await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Identification", null).ConfigureAwait(false);
                    await writer.WriteAttributeStringAsync(null, "schemeAgencyIdentifier", null, "9").ConfigureAwait(false);
                    await writer.WriteStringAsync(series.ChargeOwner.Value).ConfigureAwait(false);
                    await writer.WriteEndElementAsync().ConfigureAwait(false);

                    await writer.WriteEndElementAsync().ConfigureAwait(false);
                } // End </ChargeTypeOwnerEnergyParty>

                // <Version />
                await WriteElementIfHasValueAsync("Version", series.CalculationVersion.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            } // End PayloadEnergyTimeSeries
        }
    }
}
