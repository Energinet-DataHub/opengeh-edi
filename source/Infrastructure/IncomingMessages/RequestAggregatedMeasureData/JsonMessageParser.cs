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
using DocumentFormat = Domain.Documents.DocumentFormat;

namespace Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class JsonMessageParser : JsonParserBase<Serie, RequestAggregatedMeasureDataTransactionCommand>,
    IMessageParser<Serie, RequestAggregatedMeasureDataTransactionCommand>
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private const string DocumentName = "RequestAggregatedMeasureData";

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand>> ParseAsync(
        Stream message,
        CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand>(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        var errors = await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand>(errors.ToArray());
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            JsonElement header = document.RootElement.GetProperty(HeaderElementName);
            JsonElement seriesJson = header.GetProperty(SeriesElementName);

            return ParseJsonData(MessageHeaderFrom(header), seriesJson);
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
            element.GetProperty("mRID").ToString(),
            GetPropertyWithValue(element, "marketEvaluationPoint.type"),
            GetPropertyWithValue(element, "marketEvaluationPoint.settlementMethod"),
            element.GetProperty("start_DateAndOrTime.dateTime").ToString(),
            element.TryGetProperty("end_DateAndOrTime.dateTime", out var endDateProperty) ? endDateProperty.ToString() : null,
            GetPropertyWithValue(element, "meteringGridArea_Domain.mRID"),
            GetPropertyWithValue(element, "energySupplier_MarketParticipant.mRID"),
            GetPropertyWithValue(element, "balanceResponsibleParty_MarketParticipant.mRID"));
    }

    private static string? GetPropertyWithValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetProperty("value").ToString() : null;
    }

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand> ParseJsonData(
        MessageHeader header,
        JsonElement seriesJson)
    {
        var series = new List<Serie>();

        foreach (var jsonElement in seriesJson.EnumerateArray())
        {
            series.Add(SeriesFrom(jsonElement));
        }

        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransactionCommand>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(header, series));
    }
}
