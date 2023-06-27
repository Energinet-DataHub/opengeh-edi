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
using System.Threading;
using System.Threading.Tasks;
using Application.IncomingMessages;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Messages;
using CimMessageAdapter.ValidationErrors;
using DocumentValidation;
using Infrastructure.IncomingMessages.BaseParsers;
using Json.Schema;
using DocumentFormat = Domain.Documents.DocumentFormat;

namespace Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class JsonMessageParser : JsonParserBase<Serie, RequestAggregatedMeasureDataTransaction>, IMessageParser<Serie, RequestAggregatedMeasureDataTransaction>
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private const string DocumentName = "RequestAggregatedMeasureData";
    private const int MaxMessageSizeInMb = 50;

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseAsync(Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var fileSizeInMb = message.Length / (1024 * 1024);
        if (fileSizeInMb >= MaxMessageSizeInMb)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
                new MessageSizeExceeded(fileSizeInMb, MaxMessageSizeInMb));
        }

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);

        if (schema is null)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        var errors = GetErrors();
        if (errors.Count > 0)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(errors.ToArray());
        }

        try
        {
            JsonElement seriesJson;
            using var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            MessageHeader header = GetDocumentHeaderAndTransactions(
                document,
                HeaderElementName,
                SeriesElementName,
                out seriesJson);

            return ParseJsonData(header, seriesJson);
        }
        catch (JsonException exception)
        {
            return InvalidJsonFailure(exception);
        }
        catch (ArgumentException argumentException)
        {
            return InvalidJsonFailure(argumentException);
        }
        catch (IOException e)
        {
            return InvalidJsonFailure(e);
        }
    }

    private static Serie SeriesFrom(JsonElement element)
    {
        return new Serie(
            element.GetProperty("mRID")
                .ToString(),
            element.GetProperty("settlement_Series.version").GetProperty("value")
                .ToString(),
            element.GetProperty("marketEvaluationPoint.type").GetProperty("value")
                .ToString(),
            element.GetProperty("marketEvaluationPoint.settlementMethod").GetProperty("value")
                .ToString(),
            element.GetProperty("start_DateAndOrTime.dateTime")
                .ToString(),
            element.GetProperty("end_DateAndOrTime.dateTime")
                .ToString(),
            element.GetProperty("meteringGridArea_Domain.mRID").GetProperty("value")
                .ToString(),
            element.GetProperty("biddingZone_Domain.mRID").GetProperty("value")
                .ToString(),
            element.GetProperty("energySupplier_MarketParticipant.mRID").GetProperty("value")
                .ToString(),
            element.GetProperty("balanceResponsibleParty_MarketParticipant.mRID").GetProperty("value")
                .ToString());
    }

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction> ParseJsonData(
        MessageHeader header,
        JsonElement seriesJson)
    {
        var series = new List<Serie>();

        foreach (var jsonElement in seriesJson.EnumerateArray())
        {
            series.Add(SeriesFrom(jsonElement));
        }

        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(header, series));
    }
}
