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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;

public sealed class NotifyWholesaleServicesCimJsonDocumentWriter : IDocumentWriter
{
    private const string DocumentTypeName = "NotifyWholesaleServices_MarketDocument";
    private const string TypeCode = "E31";
    private readonly IMessageRecordParser _parser;

    public NotifyWholesaleServicesCimJsonDocumentWriter(IMessageRecordParser parser)
    {
        _parser = parser;
    }

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyWholesaleServices;
    }

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader header,
        IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MarketDocumentWriterMemoryStream();
        var options = new JsonWriterOptions { Indented = true };

        using var writer = new Utf8JsonWriter(stream, options);
        CimJsonHeaderWriter.Write(header, DocumentTypeName, TypeCode, null, writer);
        WriteSeries(marketActivityRecords, writer);
        writer.WriteEndObject();
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
        {
            foreach (var series in ParseFrom(marketActivityRecords))
            {
                writer.WriteStartObject();
                {
                    writer.WriteProperty("mRID", series.TransactionId.Value);
                    writer.WriteProperty("version", series.CalculationVersion.ToString(NumberFormatInfo.InvariantInfo));

                    if (series.SettlementVersion is not null)
                    {
                        writer.WriteObject(
                            "settlement_Series.version",
                            KeyValuePair.Create("value", series.SettlementVersion.Code));
                    }

                    if (series.OriginalTransactionIdReference is not null)
                    {
                        writer.WriteProperty("originalTransactionIDReference_Series.mRID", series.OriginalTransactionIdReference.Value);
                    }

                    if (series.MeteringPointType is not null)
                    {
                        writer.WriteObject(
                            "marketEvaluationPoint.type",
                            KeyValuePair.Create("value", series.MeteringPointType.Code));
                    }

                    if (series.SettlementMethod is not null)
                    {
                        writer.WriteObject("marketEvaluationPoint.settlementMethod", KeyValuePair.Create("value", series.SettlementMethod.Code));
                    }

                    if (series.ChargeCode is not null)
                    {
                        writer.WriteProperty("chargeType.mRID", series.ChargeCode);
                    }

                    if (series.ChargeType is not null)
                    {
                        writer.WriteObject(
                            "chargeType.type",
                            KeyValuePair.Create("value", series.ChargeType.Code));
                    }

                    if (series.ChargeOwner is not null)
                    {
                        writer.WriteObject(
                            "chargeType.chargeTypeOwner_MarketParticipant.mRID",
                            new KeyValuePair<string, string>(
                                "codingScheme",
                                CimCode.CodingSchemeOf(series.ChargeOwner)),
                            new KeyValuePair<string, string>("value", series.ChargeOwner.Value));
                    }

                    writer.WriteObject(
                        "meteringGridArea_Domain.mRID",
                        new KeyValuePair<string, string>("codingScheme", "NDK"),
                        new KeyValuePair<string, string>("value", series.GridAreaCode));

                    writer.WriteObject(
                        "energySupplier_MarketParticipant.mRID",
                        new KeyValuePair<string, string>("codingScheme", CimCode.CodingSchemeOf(series.EnergySupplier)),
                        new KeyValuePair<string, string>("value", series.EnergySupplier.Value));

                    writer.WriteProperty("product", ProductType.Tariff.Code);

                    writer.WriteObject(
                        "quantity_Measure_Unit.name",
                        KeyValuePair.Create("value", series.QuantityMeasureUnit.Code));

                    if (series.PriceMeasureUnit is not null)
                    {
                        writer.WriteObject(
                            "price_Measure_Unit.name",
                            KeyValuePair.Create("value", series.PriceMeasureUnit.Code));
                    }

                    writer.WriteObject(
                        "currency_Unit.name",
                        KeyValuePair.Create("value", series.Currency.Code));

                    writer.WritePropertyName("Period");
                    writer.WriteStartObject();
                    {
                        writer.WriteProperty("resolution", series.Resolution.Code);

                        writer.WritePropertyName("timeInterval");
                        writer.WriteStartObject();
                        {
                            writer.WriteObject(
                                "start",
                                KeyValuePair.Create("value", series.Period.StartToString()));
                            writer.WriteObject(
                                "end",
                                KeyValuePair.Create("value", series.Period.EndToString()));
                        }

                        writer.WriteEndObject();

                        writer.WritePropertyName("Point");
                        writer.WriteStartArray();
                        {
                            foreach (var point in series.Points)
                            {
                                writer.WriteStartObject();
                                {
                                    writer.WritePropertyName("energySum_Quantity.quantity");
                                    writer.WriteNumberValue(point.Amount ?? 0);

                                    if (point.Quantity is not null)
                                    {
                                        writer.WritePropertyName("energy_Quantity.quantity");
                                        writer.WriteNumberValue(point.Quantity.GetValueOrDefault());
                                    }

                                    writer.WriteObject(
                                        "position",
                                        KeyValuePair.Create("value", point.Position));

                                    if (point.Price is not null)
                                    {
                                        writer.WriteObject(
                                            "price.amount",
                                            KeyValuePair.Create(
                                                    "value",
                                                    point.Price.GetValueOrDefault()));
                                    }

                                    if (point.QuantityQuality != null)
                                    {
                                        writer.WriteObject(
                                            "quality",
                                            KeyValuePair.Create(
                                                "value",
                                                CimCode.ForWholesaleServicesOf(point.QuantityQuality.Value)));
                                    }
                                }

                                writer.WriteEndObject();
                            }
                        }

                        writer.WriteEndArray();
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private ReadOnlyCollection<WholesaleCalculationMarketActivityRecord> ParseFrom(IReadOnlyCollection<string> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        return payloads
            .Select(payload => _parser.From<WholesaleCalculationMarketActivityRecord>(payload))
            .ToList()
            .AsReadOnly();
    }
}
