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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Json.Schema;
using NJsonSchema;
using NJsonSchema.Generation;
using JsonSchema = Json.Schema.JsonSchema;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

/// <summary>
/// This class is responsible for parsing a B2C JSON message into a specific type.
/// </summary>
/// <typeparam name="T">
/// The type to parse the B2C JSON message into.
/// </typeparam>
public abstract class B2CJsonMessageParserBase<T>(ISerializer serializer) : MessageParserBase<JsonSchema>()
{
    private readonly ISerializer _serializer = serializer;

    protected override Task<(JsonSchema? Schema, ValidationError? ValidationError)> GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken)
    {
        var schema = NJsonSchema.JsonSchema.FromType<T>(
            new SystemTextJsonSchemaGeneratorSettings()
            {
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                SchemaType = SchemaType.JsonSchema,
            });

        // Serialize the NJsonSchema.JsonSchema to a JSON string
        var jsonString = schema.ToJson();

        // Add the $schema property to the JSON string
        var jsonObject = JsonDocument.Parse(jsonString).RootElement.Clone();
        var jsonSchemaString = jsonObject.GetRawText().Replace("http://json-schema.org/draft-04/schema#", "http://json-schema.org/draft-07/schema");

        // Deserialize the JSON string to a Json.Schema.JsonSchema
        var jsonSchema = JsonSerializer.Deserialize<JsonSchema>(jsonSchemaString);
        return Task.FromResult((jsonSchema, (ValidationError?)null));
    }

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        JsonSchema schemaResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(marketMessage);

        var isSchemaValidAsync = await IsSchemaValidAsync(marketMessage.Stream, schemaResult, cancellationToken)
            .ConfigureAwait(false);

        if (isSchemaValidAsync.Any())
        {
            return new IncomingMarketMessageParserResult(isSchemaValidAsync);
        }

        var requestWholesaleSettlementDto = await _serializer
            .DeserializeAsync<T>(marketMessage.Stream, cancellationToken)
            .ConfigureAwait(false);

        var incomingMessage = MapIncomingMessage(requestWholesaleSettlementDto);
        return new IncomingMarketMessageParserResult(incomingMessage);
    }

    protected abstract IIncomingMessage MapIncomingMessage(T incomingMessageDto);

    private async Task<ValidationError[]> IsSchemaValidAsync(Stream marketMessageStream, JsonSchema jsonSchema, CancellationToken cancellationToken)
    {
        var document = await JsonDocument.ParseAsync(marketMessageStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var validationResult = jsonSchema.Evaluate(document, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

        if (!validationResult.IsValid)
        {
            return validationResult.Details
                .Select(FindErrorsForInvalidEvaluation)
                .SelectMany(detail => detail)
                .ToArray();
        }

        // Reset the stream position to the beginning for deserialization
        marketMessageStream.Position = 0;

        return [];
    }

    private ValidationError[] FindErrorsForInvalidEvaluation(EvaluationResults result)
    {
        if (!result.IsValid)
        {
            foreach (var detail in result.Details)
            {
                FindErrorsForInvalidEvaluation(detail);
            }
        }

        if (!result.HasErrors || result.Errors == null) return [];

        var validationErrors = new List<ValidationError>();
        var propertyName = result.InstanceLocation.ToString();
        foreach (var error in result.Errors)
        {
            validationErrors.Add(InvalidMessageStructure.From($"{propertyName}: {error}"));
        }

        return validationErrors.ToArray();
    }
}
