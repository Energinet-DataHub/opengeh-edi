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
using System.Threading.Tasks;
using System.Xml;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.WholesaleCalculations;

public class WholesaleCalculationXmlDocumentWriter : DocumentWriter
{
    public WholesaleCalculationXmlDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
                "NotifyWholesaleServices_MarketDocument",
                "urn:ediel.org:measure:notifywholesaleservices:0:1 urn-ediel-org-measure-notifywholesaleservices-0-1.xsd",
                "urn:ediel.org:measure:notifywholesaleservices:0:1",
                "cim",
                "E31"),
            parser,
            null)
    {
    }

    protected override async Task WriteMarketActivityRecordsAsync(IReadOnlyCollection<string> marketActivityPayloads, XmlWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);
        ArgumentNullException.ThrowIfNull(writer);

        foreach (var wholesaleCalculationSeries in ParseFrom<WholesaleCalculationSeries>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, wholesaleCalculationSeries.TransactionId.ToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "version", null, "+++++++++++++").ConfigureAwait(false);

            await WriteElementIfHasValueAsync("settlement_Series.version", wholesaleCalculationSeries.SettlementVersion?.Code, writer).ConfigureAwait(false);
            //await WriteElementIfHasValueAsync("originalTransactionIDReference_Series.mRID", wholesaleCalculationSeries.OriginalTransactionIdReference, writer).ConfigureAwait(false);
            //await writer.WriteElementStringAsync(DocumentDetails.Prefix, "marketEvaluationPoint.type", null, "E17").ConfigureAwait(false);
            //await WriteElementIfHasValueAsync("marketEvaluationPoint.settlementMethod", wholesaleCalculationSeries.SettlementType is null ? null : CimCode.Of(SettlementType.From(wholesaleCalculationSeries.SettlementType)), writer).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "chargeType.mRID", null, wholesaleCalculationSeries.ChargeCode).ConfigureAwait(false); // TODO: is this the code???
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "chargeType.type", null, wholesaleCalculationSeries.ChargeType.Code).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "chargeType.chargeTypeOwner_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(wholesaleCalculationSeries.ChargeOwner.Value))).ConfigureAwait(false);
            await writer.WriteStringAsync(wholesaleCalculationSeries.ChargeOwner.Value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
            await writer.WriteStringAsync(wholesaleCalculationSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "energySupplier_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(wholesaleCalculationSeries.EnergySupplier.Value))).ConfigureAwait(false);
            await writer.WriteStringAsync(wholesaleCalculationSeries.EnergySupplier.Value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, GeneralValues.ProductCode).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, wholesaleCalculationSeries.QuantityUnit.Code).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "price_Measure_Unit.name", null, wholesaleCalculationSeries.PriceMeasureUnit.Code).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "currency_Unit.name", null, wholesaleCalculationSeries.Currency.Code).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Period", null).ConfigureAwait(false);

            // tabbed content

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "resolution", null, wholesaleCalculationSeries.Resolution.Code).ConfigureAwait(false);
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);

            // tabbed content
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "start", null, wholesaleCalculationSeries.Period.StartToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "end", null, wholesaleCalculationSeries.Period.EndToString()).ConfigureAwait(false);

            // tab removed
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            /*
            foreach (var point in wholesaleCalculationSeries.Point)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity", null, point.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo)!).ConfigureAwait(false);
                }

                await WriteQualityIfRequiredAsync(writer, point).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }*/

            // TODO: the next 3 lines should iterate over a list of points
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);

            // tabbed content
            await WriteElementAsync("position", "1", writer).ConfigureAwait(false); // TODO: FIX HARDCODING
            await WriteElementAsync("energySum_Quantity.quantity", wholesaleCalculationSeries.Quantity?.ToString(NumberFormatInfo.InvariantInfo) ?? "0", writer).ConfigureAwait(false);

            // tab removed
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            // tab removed
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            // closing off series tag
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }
}
