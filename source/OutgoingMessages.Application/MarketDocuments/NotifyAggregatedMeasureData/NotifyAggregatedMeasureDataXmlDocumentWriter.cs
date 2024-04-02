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
using Energinet.DataHub.EDI.OutgoingMessages.Application.DocumentWriters.Xml;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataXmlDocumentWriter : DocumentWriter
{
    public NotifyAggregatedMeasureDataXmlDocumentWriter(IMessageRecordParser parser)
        : base(
            new DocumentDetails(
            "NotifyAggregatedMeasureData_MarketDocument",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1 urn-ediel-org-measure-notifyaggregatedmeasuredata-0-1.xsd",
            "urn:ediel.org:measure:notifyaggregatedmeasuredata:0:1",
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

        foreach (var timeSeries in ParseFrom<TimeSeriesMarketActivityRecord>(marketActivityPayloads))
        {
            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Series", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, timeSeries.TransactionId.ToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "version", null, timeSeries.CalculationResultVersion.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);

            await WriteElementIfHasValueAsync(
                    "settlement_Series.version",
                    timeSeries.SettlementVersion is not null ? SettlementVersion.FromName(timeSeries.SettlementVersion).Code : null,
                    writer)
                .ConfigureAwait(false);
            await WriteElementIfHasValueAsync("originalTransactionIDReference_Series.mRID", timeSeries.OriginalTransactionIdReference, writer).ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "marketEvaluationPoint.type",
                    null,
                    MeteringPointType.FromName(timeSeries.MeteringPointType).Code)
                .ConfigureAwait(false);

            // TODO: This is keep for backward compatibility. Remove this in next pull request
            // only codes has length 3
            if (timeSeries.SettlementType is not null && timeSeries.SettlementType.Length == 3)
            {
                await WriteElementIfHasValueAsync(
                        "marketEvaluationPoint.settlementMethod",
                        SettlementMethod.FromCode(timeSeries.SettlementType).Code,
                        writer)
                    .ConfigureAwait(false);
            }
            else
            {
                await WriteElementIfHasValueAsync(
                        "marketEvaluationPoint.settlementMethod",
                        timeSeries.SettlementType is null ? null : SettlementMethod.FromName(timeSeries.SettlementType).Code,
                        writer)
                    .ConfigureAwait(false);
            }

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "meteringGridArea_Domain.mRID", null).ConfigureAwait(false);
            await writer.WriteAttributeStringAsync(null, "codingScheme", null, "NDK").ConfigureAwait(false);
            await writer.WriteStringAsync(timeSeries.GridAreaCode).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);

            if (timeSeries.EnergySupplierNumber is not null)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "energySupplier_MarketParticipant.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(timeSeries.EnergySupplierNumber))).ConfigureAwait(false);
                await writer.WriteStringAsync(timeSeries.EnergySupplierNumber).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            if (timeSeries.BalanceResponsibleNumber is not null)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "balanceResponsibleParty_MarketParticipant.mRID", null).ConfigureAwait(false);
                await writer.WriteAttributeStringAsync(null, "codingScheme", null, CimCode.CodingSchemeOf(ActorNumber.Create(timeSeries.BalanceResponsibleNumber))).ConfigureAwait(false);
                await writer.WriteStringAsync(timeSeries.BalanceResponsibleNumber).ConfigureAwait(false);
                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "product", null, ProductType.EnergyActive.Code).ConfigureAwait(false);

            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "quantity_Measure_Unit.name",
                    null,
                    MeasurementUnit.FromName(timeSeries.MeasureUnitType).Code)
                .ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Period", null).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "resolution", null, Resolution.FromName(timeSeries.Resolution).Code).ConfigureAwait(false);

            await writer.WriteStartElementAsync(DocumentDetails.Prefix, "timeInterval", null).ConfigureAwait(false);

            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "start", null, timeSeries.Period.StartToString()).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "end", null, timeSeries.Period.EndToString()).ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
            foreach (var point in timeSeries.Point)
            {
                await writer.WriteStartElementAsync(DocumentDetails.Prefix, "Point", null).ConfigureAwait(false);
                await writer.WriteElementStringAsync(DocumentDetails.Prefix, "position", null, point.Position.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
                if (point.Quantity is not null)
                {
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity", null, point.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo)!).ConfigureAwait(false);
                }

                await WriteQualityIfRequiredAsync(writer, point).ConfigureAwait(false);

                await writer.WriteEndElementAsync().ConfigureAwait(false);
            }

            await writer.WriteEndElementAsync().ConfigureAwait(false);
            await writer.WriteEndElementAsync().ConfigureAwait(false);
        }
    }

    private Task WriteQualityIfRequiredAsync(XmlWriter writer, Point point)
    {
        return point.QuantityQuality == CalculatedQuantityQuality.Measured
            ? Task.CompletedTask
            : writer.WriteElementStringAsync(
            DocumentDetails.Prefix,
            "quality",
            null,
            CimCode.Of(point.QuantityQuality));
    }
}
