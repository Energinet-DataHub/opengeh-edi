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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Xml;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;

public class NotifyAggregatedMeasureDataCimXmlDocumentWriter : CimXmlDocumentWriter
{
    public NotifyAggregatedMeasureDataCimXmlDocumentWriter(IMessageRecordParser parser)
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
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "mRID", null, timeSeries.TransactionId.Value).ConfigureAwait(false);
            await writer.WriteElementStringAsync(DocumentDetails.Prefix, "version", null, timeSeries.CalculationResultVersion.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);

            await WriteElementIfHasValueAsync(
                    "settlement_Series.version",
                    timeSeries.SettlementVersion is not null ? SettlementVersion.FromName(timeSeries.SettlementVersion).Code : null,
                    writer)
                .ConfigureAwait(false);
            await WriteElementIfHasValueAsync("originalTransactionIDReference_Series.mRID", timeSeries.OriginalTransactionIdReference?.Value, writer).ConfigureAwait(false);
            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "marketEvaluationPoint.type",
                    null,
                    MeteringPointType.FromName(timeSeries.MeteringPointType).Code)
                .ConfigureAwait(false);

            await WriteElementIfHasValueAsync(
                        "marketEvaluationPoint.settlementMethod",
                        timeSeries.SettlementMethod is not null ? SettlementMethod.FromName(timeSeries.SettlementMethod).Code : null,
                        writer)
                    .ConfigureAwait(false);

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

            var outdatedValue = string.Equals(timeSeries.MeasureUnitType, "Kwh", StringComparison.InvariantCultureIgnoreCase);
            await writer.WriteElementStringAsync(
                    DocumentDetails.Prefix,
                    "quantity_Measure_Unit.name",
                    null,
                    outdatedValue ? MeasurementUnit.KilowattHour.Code : MeasurementUnit.FromName(timeSeries.MeasureUnitType).Code)
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
                    await writer.WriteElementStringAsync(DocumentDetails.Prefix, "quantity", null, point.Quantity.Value.ToString(NumberFormatInfo.InvariantInfo)).ConfigureAwait(false);
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
            CimCode.ForEnergyResultOf(point.QuantityQuality));
    }
}
