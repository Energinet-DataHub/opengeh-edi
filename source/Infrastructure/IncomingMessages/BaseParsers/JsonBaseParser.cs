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
using CimMessageAdapter.Messages;
using CimMessageAdapter.ValidationErrors;
using DocumentValidation;
using Json.Schema;

namespace Infrastructure.IncomingMessages.BaseParsers;

public abstract class JsonParserBase<TTransactionType, TICommand>
where TTransactionType : IMarketActivityRecord
where TICommand : IMarketTransaction<TTransactionType>
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    protected JsonParserBase(ISchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    protected Task<JsonSchema?> GetSchemaAsync(string documentName, CancellationToken cancellationToken)
    {
        if (documentName == null)
        {
            throw new ArgumentNullException(nameof(documentName));
        }

        return _schemaProvider.GetSchemaAsync<JsonSchema>(documentName.ToUpper(CultureInfo.InvariantCulture), "0", cancellationToken);
    }

    protected MessageHeader MessageHeaderFrom(JsonElement element)
    {
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

    protected void ResetMessagePosition(Stream message)
    {
        if (message is { CanRead: true } && message.Position > 0)
            message.Position = 0;
    }

    protected MessageParserResult<TTransactionType, TICommand> InvalidJsonFailure(
        Exception exception)
    {
        return new MessageParserResult<TTransactionType, TICommand>(
            InvalidMessageStructure.From(exception));
    }

    protected async Task<List<ValidationError>> ValidateMessageAsync(JsonSchema schema, Stream message)
    {
        var jsonDocument = await JsonDocument.ParseAsync(message).ConfigureAwait(false);

        if (IsValid(jsonDocument, schema) == false)
        {
            ExtractValidationErrors(jsonDocument, schema);
        }

        ResetMessagePosition(message);

        return _errors;
    }

    protected string GetJsonDateStringWithoutQuotes(JsonElement element)
    {
        return element.ToString().Trim('"');
    }

    private static bool IsValid(JsonDocument document, JsonSchema schema)
    {
        return schema.Evaluate(document, new EvaluationOptions() { OutputFormat = OutputFormat.Flag, }).IsValid;
    }

    private void ExtractValidationErrors(JsonDocument jsonDocument, JsonSchema schema)
    {
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions() { OutputFormat = OutputFormat.List, });

        var errors = result
            .Details
            .Where(detail => detail.HasErrors).ToList();

        // As of right now, if an attribute is given a type, which does not specified the schema.
        // Then we will get errors in all other attributes, by the json validator.
        // When we get a type error, then the detail describing the "true" error will contain 2 errors.
        // Namely:
        // One describing the error and one saying: "Expected value to match one of the values specified by the enum"
        // The "false" errors will not have a description of what went wrong, but it will contain
        // the error: "Expected value to match one of the values specified by the enum"
        var attributeTypeErrors = errors?.Where(detail => detail.Errors?.Count > 1).ToList() ?? new List<EvaluationResults>();

        // If we have errors in the attribute types, then we will only report these errors.
        // Hence if a value of another attribute has a length which does not satisfy the schema.
        // Then the customer will have these reported when the types of the attributes matches the schema.
        if (attributeTypeErrors.Any())
        {
            var uniqueErrors = new List<EvaluationResults>();
            foreach (var error in attributeTypeErrors)
            {
                // We get more than one error for mismatching attributes types, due to some "oneOf"
                // checks done by JsonSchema. Hence we may remove these duplicated errors comparing
                // the attribute location (name).
                // e.g. InstanceLocation may be: /RequestAggregatedMeasureData_MarketDocument/businessSector.type/value
                if (uniqueErrors.All(er => er.InstanceLocation != error.InstanceLocation))
                {
                    uniqueErrors.Add(error);
                }
            }

            uniqueErrors.ForEach(AddValidationErrors);
        }
        else
        {
            errors?.ForEach(AddValidationErrors);
        }
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

    private void AddValidationError(string errorMessage)
    {
        _errors.Add(InvalidMessageStructure.From(errorMessage));
    }
}
