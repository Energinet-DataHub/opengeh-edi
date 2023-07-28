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
    private readonly string _unUsefulError = "[enum, Expected value to match one of the values specified by the enum]";

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

        errors?.ForEach(AddValidationErrors);
    }

    private void AddValidationErrors(EvaluationResults validationResult)
    {
        var propertyName = validationResult.InstanceLocation.ToString();
        var errorsValues = validationResult.Errors ?? new Dictionary<string, string>();
        foreach (var error in errorsValues)
        {
            var errorMessage = $"{propertyName}: {error}";
            if ($"{error}" != _unUsefulError && _errors.All(er => er.Message != errorMessage))
            {
                AddValidationError(errorMessage);
            }
        }
    }

    private void AddValidationError(string errorMessage)
    {
        _errors.Add(InvalidMessageStructure.From(errorMessage));
    }
}
