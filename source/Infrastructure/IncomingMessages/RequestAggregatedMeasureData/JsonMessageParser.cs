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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.IncomingMessages;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Messages;
using CimMessageAdapter.ValidationErrors;
using DocumentValidation;
using Json.Schema;
using DocumentFormat = Domain.Documents.DocumentFormat;

namespace Infrastructure.IncomingMessages.RequestAggregatedMeasureData;

public class JsonMessageParser : IMessageParser<Serie, RequestAggregatedMeasureDataTransaction>
{
    private const string SeriesRecordElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private const string DocumentName = "RequestAggregatedMeasureData";
    private const int MaxMessageSizeInMb = 50;
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>> ParseAsync(Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var fileSizeInMb = message.Length / (1024 * 1024);
        if (fileSizeInMb >= MaxMessageSizeInMb)
        {
            _errors.Add(new MessageSizeExceeded(fileSizeInMb, MaxMessageSizeInMb));
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(_errors.ToArray());
        }

        var schema = await _schemaProvider
            .GetSchemaAsync<JsonSchema>(DocumentName.ToUpper(CultureInfo.InvariantCulture), "0", cancellationToken)
            .ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (_errors.Count > 0)
        {
            return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(_errors.ToArray());
        }

        try
        {
            return ParseJsonData(
                await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
        }
        catch (JsonException exception)
        {
            return InvalidJsonFailure(exception);
        }
        catch (ArgumentException argumentException)
        {
            return InvalidJsonFailure(argumentException);
        }
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static MessageHeader MessageHeaderFrom(JsonElement element)
    {
        // flyt til en hjælper klasse
        return new MessageHeader(
            element.GetProperty("mRID").ToString(),
            element.GetProperty("type").GetProperty("value").ToString(),
            element.GetProperty("process.processType").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            GetJsonDateStringWithoutQuotes(element.GetProperty("createdDateTime")));
    }

    private static string GetJsonDateStringWithoutQuotes(JsonElement element)
    {
        return element.ToString().Trim('"');
    }

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction> ParseJsonData(
        JsonDocument document)
    {
        var series = new List<Serie>();
        var messageHeader = MessageHeaderFrom(document.RootElement.GetProperty(HeaderElementName));
        var incomingSeries = document.RootElement.GetProperty(HeaderElementName)
            .GetProperty(SeriesRecordElementName);

        foreach (var jsonElement in incomingSeries.EnumerateArray())
        {
            series.Add(SeriesFrom(jsonElement));
        }

        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
            new RequestAggregatedMeasureDataIncomingMarketDocument(messageHeader, series));
    }

    private static MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction> InvalidJsonFailure(
        Exception exception)
    {
        return new MessageParserResult<Serie, RequestAggregatedMeasureDataTransaction>(
            InvalidMessageStructure.From(exception));
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

    private static bool IsValid(JsonDocument document, JsonSchema schema)
    {
        return schema.Evaluate(document, new EvaluationOptions() { OutputFormat = OutputFormat.Flag, }).IsValid;
    }

    private async Task ValidateMessageAsync(JsonSchema schema, Stream message)
    {
        var jsonDocument = await JsonDocument.ParseAsync(message).ConfigureAwait(false);

        if (IsValid(jsonDocument, schema) == false)
        {
            ExtractValidationErrors(jsonDocument, schema);
        }

        ResetMessagePosition(message);
    }

    private void ExtractValidationErrors(JsonDocument jsonDocument, JsonSchema schema)
    {
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions() { OutputFormat = OutputFormat.List, });
        result
            .Details
            .Where(detail => detail.HasErrors)
            .ToList().ForEach(AddValidationErrors);
    }

    private void AddValidationErrors(EvaluationResults validationResult)
    {
        var propertyName = validationResult.InstanceLocation.ToString();
        var errorsValues = validationResult.Errors ?? new Dictionary<string, string>();
        foreach (var error in errorsValues)
        {
            AddValidationError($"{propertyName}: {error}");
        }
    }

    private void AddValidationError(string? errorMessage)
    {
        if (errorMessage != null)
        {
            _errors.Add(InvalidMessageStructure.From(errorMessage));
        }
    }
}
