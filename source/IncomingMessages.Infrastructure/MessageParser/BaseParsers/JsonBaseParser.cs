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

using System.Globalization;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Json.Schema;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers;

public abstract class JsonParserBase
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    protected JsonParserBase(ISchemaProvider schemaProvider)
    {
        _schemaProvider = schemaProvider;
    }

    protected static IncomingMarketMessageParserResult InvalidJsonFailure(
        Exception exception)
    {
        return new IncomingMarketMessageParserResult(
            InvalidMessageStructure.From(exception));
    }

    protected static MessageHeader MessageHeaderFrom(JsonElement element)
    {
        return new MessageHeader(
            element.GetProperty("mRID").ToString(),
            element.GetProperty("type").GetProperty("value").ToString(),
            element.GetProperty("process.processType").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("sender_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.mRID").GetProperty("value").ToString(),
            element.GetProperty("receiver_MarketParticipant.marketRole.type").GetProperty("value").ToString(),
            GetJsonDateStringWithoutQuotes(element.GetProperty("createdDateTime")),
            GetBusinessType(element));
    }

    protected Task<JsonSchema?> GetSchemaAsync(string documentName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(documentName);

        return _schemaProvider.GetSchemaAsync<JsonSchema>(documentName.ToUpper(CultureInfo.InvariantCulture), "0", cancellationToken);
    }

    protected async Task<List<ValidationError>> ValidateMessageAsync(JsonSchema schema, IIncomingMarketMessageStream marketMessage)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);

        var jsonDocument = await JsonDocument.ParseAsync(marketMessage.Stream).ConfigureAwait(false);

        if (IsValid(jsonDocument, schema) == false)
        {
            ExtractValidationErrors(jsonDocument, schema);
        }

        return _errors.DistinctBy(x => x.Message).ToList();
    }

    private static string GetJsonDateStringWithoutQuotes(JsonElement element)
    {
        return element.ToString().Trim('"');
    }

    private static bool IsValid(JsonDocument document, JsonSchema schema)
    {
        return schema.Evaluate(document, new EvaluationOptions() { OutputFormat = OutputFormat.Flag, }).IsValid;
    }

    private static string? GetBusinessType(JsonElement element)
    {
        return element.TryGetProperty("businessSector.type", out var property) ? property.GetProperty("value").ToString() : null;
    }

    private void ExtractValidationErrors(JsonDocument jsonDocument, JsonSchema schema)
    {
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions() { OutputFormat = OutputFormat.Hierarchical, });
        FindErrorsForInvalidEvaluation(result);
    }

    private void FindErrorsForInvalidEvaluation(EvaluationResults result)
    {
        if (!result.IsValid)
        {
            foreach (var detail in result.Details)
            {
                FindErrorsForInvalidEvaluation(detail);
            }
        }

        if (!result.HasErrors || result.Errors == null) return;

        var propertyName = result.InstanceLocation.ToString();
        foreach (var error in result.Errors)
        {
            AddValidationError($"{propertyName}: {error}");
        }
    }

    private void AddValidationError(string errorMessage)
    {
        _errors.Add(InvalidMessageStructure.From(errorMessage));
    }
}
