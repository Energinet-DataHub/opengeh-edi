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
using Application.IncomingMessages.RequestChangeOfSupplier;
using CimMessageAdapter.Errors;
using CimMessageAdapter.Messages;
using DocumentValidation;
using Json.Schema;
using DocumentFormat = Domain.Documents.DocumentFormat;
using MessageHeader = Application.IncomingMessages.MessageHeader;

namespace Infrastructure.IncomingMessages.RequestChangeOfSupplier;

public class JsonMessageParser : IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private const string DocumentName = "RequestChangeOfSupplier";
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>> ParseAsync(
        Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var schema = await _schemaProvider
            .GetSchemaAsync<JsonSchema>(DocumentName.ToUpper(CultureInfo.InvariantCulture), "0", cancellationToken)
            .ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(
                new UnknownBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (_errors.Count > 0)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(_errors.ToArray());
        }

        try
        {
            using (var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken)
                       .ConfigureAwait(false))
            {
                try
                {
                    return ParseJsonData(document);
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
        }
        catch (IOException e)
        {
            return InvalidJsonFailure(e);
        }
        finally
        {
        }
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> ParseJsonData(
        JsonDocument document)
    {
        var marketActivityRecords = new List<MarketActivityRecord>();
        var messageHeader = MessageHeaderFrom(document.RootElement.GetProperty(HeaderElementName));
        var marketActivityRecord = document.RootElement.GetProperty(HeaderElementName)
            .GetProperty(MarketActivityRecordElementName);
        var records = marketActivityRecord.EnumerateArray();
        foreach (var jsonElement in records)
        {
            marketActivityRecords.Add(MarketActivityRecordFrom(jsonElement));
        }

        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(
            new RequestChangeOfSupplierIncomingMarketDocument(messageHeader, marketActivityRecords));
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> InvalidJsonFailure(
        Exception exception)
    {
        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(
            InvalidMessageStructure.From(exception));
    }

    private static MessageHeader MessageHeaderFrom(JsonElement element)
    {
        return new MessageHeader(
            element.GetProperty("mRID").ToString(),
            element.GetProperty("process.processType").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            GetJsonDateStringWithoutQuotes(element.GetProperty("createdDateTime")));
    }

    private static MarketActivityRecord MarketActivityRecordFrom(JsonElement element)
    {
        return new MarketActivityRecord()
        {
            Id = element.GetProperty("mRID").ToString(),
            ConsumerId =
                element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("value")
                    .ToString(),
            ConsumerIdType =
                element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("codingScheme")
                    .ToString(),
            BalanceResponsibleId =
                element.GetProperty("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID")
                    .GetProperty("value").ToString(),
            EnergySupplierId =
                element.GetProperty("marketEvaluationPoint.energySupplier_MarketParticipant.mRID").GetProperty("value")
                    .ToString(),
            MarketEvaluationPointId = element.GetProperty("marketEvaluationPoint.mRID").GetProperty("value").ToString(),
            ConsumerName = element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.name").ToString(),
            EffectiveDate = GetJsonDateStringWithoutQuotes(element.GetProperty("start_DateAndOrTime.dateTime")),
        };
    }

    private static string GetJsonDateStringWithoutQuotes(JsonElement element)
    {
        return element.ToString().Trim('"');
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
