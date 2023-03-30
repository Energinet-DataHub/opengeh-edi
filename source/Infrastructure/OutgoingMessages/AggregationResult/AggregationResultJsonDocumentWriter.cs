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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.Common.Json;
using Json.More;
using DocumentFormat = Domain.OutgoingMessages.DocumentFormat;

namespace Infrastructure.OutgoingMessages.AggregationResult;

public class AggregationResultJsonDocumentWriter : IMessageWriter
{
    private const string ActiveEnergy = "8716867000030";
    private const string DocumentType = "NotifyAggregatedMeasureData_MarketDocument";
    private const string TypeCode = "E31";
    private readonly IMessageRecordParser _parser;

    public AggregationResultJsonDocumentWriter(IMessageRecordParser parser)
    {
        _parser = parser;
    }

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(MessageType messageType)
    {
        return messageType == MessageType.NotifyAggregatedMeasureData;
    }

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);

        JsonHeaderWriter.Write(header, DocumentType, TypeCode, null, writer);
        WriteSeries(marketActivityRecords, writer);
        writer.WriteEndObject();
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    private void WriteSeries(IReadOnlyCollection<string> marketActivityRecords, Utf8JsonWriter writer)
    {
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WritePropertyName("Series");
        writer.WriteStartArray();

        foreach (var series in ParseFrom(marketActivityRecords))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("mRID");
            writer.WriteStringValue(series.TransactionId);

            writer.WritePropertyName("meteringGridArea_Domain.mRID");
            writer.WriteStartObject();
            writer.WritePropertyName("codingScheme");
            writer.WriteStringValue("NDK");
            writer.WritePropertyName("value");
            writer.WriteStringValue(series.GridAreaCode);
            writer.WriteEndObject();

            if (series.BalanceResponsibleNumber is not null)
            {
                writer.WritePropertyName("balanceResponsibleParty_MarketParticipant.mRID");
                writer.WriteStartObject();
                writer.WritePropertyName("codingScheme");
                writer.WriteStringValue(CimCode.CodingSchemeOf(ActorNumber.Create(series.BalanceResponsibleNumber)));
                writer.WritePropertyName("value");
                writer.WriteStringValue(series.BalanceResponsibleNumber);
                writer.WriteEndObject();
            }

            if (series.EnergySupplierNumber is not null)
            {
                writer.WritePropertyName("energySupplier_MarketParticipant.mRID");
                writer.WriteStartObject();
                writer.WritePropertyName("codingScheme");
                writer.WriteStringValue(CimCode.CodingSchemeOf(ActorNumber.Create(series.EnergySupplierNumber)));
                writer.WritePropertyName("value");
                writer.WriteStringValue(series.EnergySupplierNumber);
                writer.WriteEndObject();
            }

            writer.WritePropertyName("marketEvaluationPoint.type");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(CimCode.Of(MeteringPointType.From(series.MeteringPointType)));
            writer.WriteEndObject();

            writer.WritePropertyName("product");
            writer.WriteStringValue(ActiveEnergy);

            writer.WritePropertyName("quantity_Measure_Unit.name");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(CimCode.Of(MeasurementUnit.From(series.MeasureUnitType)));
            writer.WriteEndObject();

            writer.WritePropertyName("Period");
            writer.WriteStartObject();
            writer.WritePropertyName("resolution");
            writer.WriteStringValue(CimCode.Of(Resolution.From(series.Resolution)));

            writer.WritePropertyName("timeInterval");
            writer.WriteStartObject();
            writer.WritePropertyName("start");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(series.Period.Start.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture));
            writer.WriteEndObject();
            writer.WritePropertyName("end");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteStringValue(series.Period.End.ToString("yyyy-MM-ddTHH:mm'Z'", CultureInfo.InvariantCulture));
            writer.WriteEndObject();
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
                if (point.Quantity.HasValue)
                {
                    writer.WritePropertyName("quantity");
                    writer.WriteNumberValue(point.Quantity.GetValueOrDefault());
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            // Points
            writer.WriteEndObject(); // Period end

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private IReadOnlyCollection<TimeSeries> ParseFrom(IReadOnlyCollection<string> payloads)
    {
        if (payloads == null) throw new ArgumentNullException(nameof(payloads));
        var timeSeries = new List<TimeSeries>();
        foreach (var payload in payloads)
        {
            timeSeries.Add(_parser.From<TimeSeries>(payload));
        }

        return timeSeries;
    }
}
