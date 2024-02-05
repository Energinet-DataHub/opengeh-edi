﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;

public class AggregationResultJsonDocumentWriter : IDocumentWriter
{
    private const string DocumentTypeName = "NotifyAggregatedMeasureData_MarketDocument";
    private const string TypeCode = "E31";
    private readonly IMessageRecordParser _parser;

    public AggregationResultJsonDocumentWriter(IMessageRecordParser parser)
    {
        _parser = parser;
    }

#pragma warning disable CA1822
    public bool HandlesFormat(DocumentFormat format)
#pragma warning restore CA1822
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyAggregatedMeasureData;
    }

    public async Task<MarketDocumentStream> WriteAsync(OutgoingMessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MarketDocumentWriterMemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);

        JsonHeaderWriter.Write(header, DocumentTypeName, TypeCode, null, writer);
        WriteSeries(marketActivityRecords, writer);
        writer.WriteEndObject();
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return new MarketDocumentStream(stream);
    }

    private void WriteSeries(IReadOnlyCollection<string> marketActivityRecords, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityRecords);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WritePropertyName("Series");
        writer.WriteStartArray();

        foreach (var series in ParseFrom(marketActivityRecords))
        {
            writer.WriteStartObject();

            writer.WriteProperty("mRID", series.TransactionId.ToString());
            if (series.CalculationResultVersion.HasValue)
            {
                writer.WriteProperty("version", series.CalculationResultVersion.Value.ToString(NumberFormatInfo.InvariantInfo));
            }
            else
            {
                writer.WriteProperty("version", "1");
            }

            writer.WriteObject(
                "meteringGridArea_Domain.mRID",
                new KeyValuePair<string, string>("codingScheme", "NDK"),
                new KeyValuePair<string, string>("value", series.GridAreaCode));

            if (series.BalanceResponsibleNumber is not null)
            {
                writer.WriteObject(
                    "balanceResponsibleParty_MarketParticipant.mRID",
                    new KeyValuePair<string, string>("codingScheme", CimCode.CodingSchemeOf(ActorNumber.Create(series.BalanceResponsibleNumber))),
                    new KeyValuePair<string, string>("value", series.BalanceResponsibleNumber));
            }

            if (series.EnergySupplierNumber is not null)
            {
                writer.WriteObject(
                    "energySupplier_MarketParticipant.mRID",
                    new KeyValuePair<string, string>("codingScheme", CimCode.CodingSchemeOf(ActorNumber.Create(series.EnergySupplierNumber))),
                    new KeyValuePair<string, string>("value", series.EnergySupplierNumber));
            }

            if (series.SettlementType is not null)
            {
                writer.WriteObject(
                    "marketEvaluationPoint.settlementMethod",
                    new KeyValuePair<string, string>("value", CimCode.Of(SettlementType.From(series.SettlementType))));
            }

            if (series.OriginalTransactionIdReference is not null)
            {
                writer.WriteProperty("originalTransactionIDReference_Series.mRID", series.OriginalTransactionIdReference);
            }

            writer.WriteObject("marketEvaluationPoint.type", new KeyValuePair<string, string>("value", CimCode.Of(MeteringPointType.From(series.MeteringPointType))));
            writer.WriteProperty("product", GeneralValues.ProductCode);
            writer.WriteObject("quantity_Measure_Unit.name", new KeyValuePair<string, string>("value", CimCode.Of(MeasurementUnit.From(series.MeasureUnitType))));

            if (series.SettlementVersion is not null)
            {
                writer.WriteObject("settlement_Series.version", new KeyValuePair<string, string>("value", CimCode.Of(SettlementVersion.FromName(series.SettlementVersion))));
            }

            writer.WritePropertyName("Period");
            writer.WriteStartObject();

            writer.WriteProperty("resolution", CimCode.Of(Resolution.From(series.Resolution)));

            writer.WritePropertyName("timeInterval");
            writer.WriteStartObject();
            writer.WriteObject("start", new KeyValuePair<string, string>("value", series.Period.StartToString()));
            writer.WriteObject("end", new KeyValuePair<string, string>("value", series.Period.EndToString()));
            writer.WriteEndObject();

            // Points
            writer.WritePropertyName("Point");
            writer.WriteStartArray();
            foreach (var point in series.Point)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("position");
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteNumberValue(point.Position);
                writer.WriteEndObject();

                if (point.QuantityQuality != CalculatedQuantityQuality.Measured)
                {
                    writer.WriteObject(
                        "quality",
                        new KeyValuePair<string, string>("value", CimCode.Of(point.QuantityQuality)));
                }

                if (point.Quantity.HasValue)
                {
                    writer.WritePropertyName("quantity");
                    writer.WriteNumberValue(point.Quantity.GetValueOrDefault());
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private ReadOnlyCollection<TimeSeriesMarketActivityRecord> ParseFrom(IReadOnlyCollection<string> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);
        var timeSeries = new List<TimeSeriesMarketActivityRecord>();
        foreach (var payload in payloads)
        {
            timeSeries.Add(_parser.From<TimeSeriesMarketActivityRecord>(payload));
        }

        return timeSeries.AsReadOnly();
    }
}
