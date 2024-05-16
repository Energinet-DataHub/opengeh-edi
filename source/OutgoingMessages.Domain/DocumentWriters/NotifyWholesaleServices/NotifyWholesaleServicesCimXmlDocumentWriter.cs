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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Xml;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;

public class NotifyWholesaleServicesCimXmlDocumentWriter : CimXmlDocumentWriter
{
    public NotifyWholesaleServicesCimXmlDocumentWriter(IMessageRecordParser parser)
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

        foreach (var wholesaleCalculationSeries in ParseFrom<WholesaleCalculationMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, wholesaleCalculationSeries.TransactionId.Value).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "version", null, wholesaleCalculationSeries.CalculationVersion.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);

            await WriteElementIfHasValueAsync("settlement_Series.version", wholesaleCalculationSeries.SettlementVersion?.Code, writer).ConfigureAwait(false);

            // These are there for later use, but are not used as of right now
            await WriteElementIfHasValueAsync("originalTransactionIDReference_Series.mRID", wholesaleCalculationSeries.OriginalTransactionIdReference?.Value, writer).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("marketEvaluationPoint.type", wholesaleCalculationSeries.MeteringPointType?.Code, writer).ConfigureAwait(false);
#pragma warning disable CS0618 // Type or member is obsolete
            await WriteElementIfHasValueAsync("marketEvaluationPoint.settlementMethod", wholesaleCalculationSeries.SettlementType?.Code ?? wholesaleCalculationSeries.SettlementMethod?.Code, writer).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

            await WriteElementIfHasValueAsync("chargeType.mRID", wholesaleCalculationSeries.ChargeCode, writer).ConfigureAwait(false);
            await WriteElementIfHasValueAsync("chargeType.type", wholesaleCalculationSeries.ChargeType?.Code, writer).ConfigureAwait(false);

            if (wholesaleCalculationSeries.ChargeOwner is not null)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "chargeType.chargeTypeOwner_MarketParticipant.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(wholesaleCalculationSeries.ChargeOwner.Value))).ConfigureAwait(false);
                await writer.WriteStringAsync(wholesaleCalculationSeries.ChargeOwner.Value).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
            await writer.WriteStringAsync(wholesaleCalculationSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "energySupplier_MarketParticipant.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(wholesaleCalculationSeries.EnergySupplier.Value))).ConfigureAwait(false);
            await writer.WriteStringAsync(wholesaleCalculationSeries.EnergySupplier.Value).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, ProductType.Tariff.Code).ConfigureAwait(false);

#pragma warning disable CS0618 // Type or member is obsolete
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity_Measure_Unit.name", null, wholesaleCalculationSeries.QuantityUnit?.Code ?? wholesaleCalculationSeries.QuantityMeasureUnit.Code).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

            await WriteElementIfHasValueAsync("price_Measure_Unit.name", wholesaleCalculationSeries.PriceMeasureUnit?.Code, writer).ConfigureAwait(false);
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

            foreach (var point in wholesaleCalculationSeries.Points)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);

                await WriteElementAsync("position", point.Position.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

                await WriteElementIfHasValueAsync("energy_Quantity.quantity", point.Quantity?.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

                await WriteElementIfHasValueAsync("price.amount", point.Price?.ToString(NumberFormatInfo.InvariantInfo), writer).ConfigureAwait(false);

                // energySum_Quantity.quantity is nullable according to the schema, but as of right now. Things do not make sense if it is not present
                await WriteElementAsync("energySum_Quantity.quantity", point.Amount?.ToString(NumberFormatInfo.InvariantInfo) ?? "0", writer).ConfigureAwait(false);

                await WriteQualityIfSpecifiedAsync(writer, point).ConfigureAwait(false);

                // tab removed
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            // tab removed
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            // closing off series tag
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private Task WriteQualityIfSpecifiedAsync(XmlWriter writer, Point point)
    {
        return point.QuantityQuality == null
            ? Task.CompletedTask
            : writer.WriteElementStringAsync(
                DocumentDetails.Prefix,
                "quality",
                null,
                CimCode.ForWholesaleServicesOf(point.QuantityQuality!.Value));
    }
}
