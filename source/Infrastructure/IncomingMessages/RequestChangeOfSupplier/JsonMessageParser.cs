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
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml;
using Application.IncomingMessages.RequestChangeOfSupplier;
using CimMessageAdapter.Errors;
using CimMessageAdapter.Messages;
using DocumentValidation;
using Domain.OutgoingMessages;
using Json.Schema;
using MessageHeader = Application.IncomingMessages.MessageHeader;

namespace Infrastructure.IncomingMessages.RequestChangeOfSupplier;

public class JsonMessageParser : IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    public MessageFormat HandledFormat => MessageFormat.Json;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>> ParseAsync(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string processType = "RequestChangeOfSupplier";

        /*
        try
        {
            processType = GetBusinessProcessType(document);
        }
        catch (JsonException exception)
        {
            return InvalidJsonFailure(exception);
        }
        */

        var schema = await _schemaProvider.GetSchemaAsync<JsonSchema>(processType.ToUpper(CultureInfo.InvariantCulture), "0").ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(new UnknownBusinessProcessTypeOrVersion(processType, "0"));
        }

        ResetMessagePosition(message);

        await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (_errors.Count > 0)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(_errors.ToArray());
        }

        try
        {
            using (var document = await JsonDocument.ParseAsync(message).ConfigureAwait(false))
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

    private static string[] SplitNamespace(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string[] split;
        ResetMessagePosition(message);
        var options = new JsonSerializerOptions();
        var deserialized = JsonSerializer.Deserialize<JsonObject>(message, options);
        if (deserialized is null) throw new InvalidOperationException("Unable to read first node");
        var path = deserialized.First().Value?.ToString();
        if (path is null) throw new InvalidOperationException("Unable to read path");
        split = path.Split('_');

        return split;
    }

    /*
    private static string GetBusinessProcessType(JsonDocument document)
    {
        //if (message == null) throw new ArgumentNullException(nameof(message));
        //var split = SplitNamespace(message);
        var processType = document.RootElement.GetProperty()
        return processType;
    }
    */

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> ParseJsonData(JsonDocument document)
    {
        var marketActivityRecords = new List<MarketActivityRecord>();
        var messageHeader = MessageHeaderFrom(document.RootElement.GetProperty(HeaderElementName));
        var marketActivityRecord = document.RootElement.GetProperty(HeaderElementName).GetProperty(MarketActivityRecordElementName);
        var records = marketActivityRecord.EnumerateArray();
        foreach (var jsonElement in records)
        {
            marketActivityRecords.Add(MarketActivityRecordFrom(jsonElement));
        }

        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(new RequestChangeOfSupplierIncomingMarketDocument(messageHeader, marketActivityRecords));
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> InvalidJsonFailure(Exception exception)
    {
        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(InvalidMessageStructure.From(exception));
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
            ConsumerId = element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("value").ToString(),
            ConsumerIdType = element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("codingScheme").ToString(),
            BalanceResponsibleId = element.GetProperty("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID").GetProperty("value").ToString(),
            EnergySupplierId = element.GetProperty("marketEvaluationPoint.energySupplier_MarketParticipant.mRID").GetProperty("value").ToString(),
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
