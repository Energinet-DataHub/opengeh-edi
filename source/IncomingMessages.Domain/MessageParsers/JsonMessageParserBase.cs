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
using System.Text.Json.Nodes;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Json.More;
using Json.Schema;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

public abstract class JsonMessageParserBase(
    JsonSchemaProvider schemaProvider,
    ILogger<JsonMessageParserBase> logger) : MessageParserBase<JsonSchema>()
{
    private const string ValueElementName = "value";
    private const string IdentificationElementName = "mRID";
    private const string SeriesElementName = "Series";
    private const string MessageTypeElementName = "type";
    private const string ProcessTypeElementName = "process.processType";
    private const string SenderIdentificationElementName = "sender_MarketParticipant.mRID";
    private const string ReceiverIdentificationElementName = "receiver_MarketParticipant.mRID";
    private const string SenderRoleElementName = "sender_MarketParticipant.marketRole.type";
    private const string ReceiverRoleElementName = "receiver_MarketParticipant.marketRole.type";
    private const string CreatedDateElementName = "createdDateTime";
    private const string BusinessSectorTypeElementName = "businessSector.type";

    // JsonSchema.Net optimizes repeated evaluations with the same schema by performing some static analysis
    // during the first evaluation.
    // https://docs.json-everything.net/schema/basics/#schema-options
    private static readonly EvaluationOptions _cachedEvaluationOptions = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
    };

    private readonly JsonSchemaProvider _schemaProvider = schemaProvider;
    private readonly ILogger<JsonMessageParserBase> _logger = logger;

    protected abstract string HeaderElementName { get; }

    protected abstract string DocumentName { get; }

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        JsonSchema schemaResult,
        CancellationToken cancellationToken)
    {
        using var document = await TryParseJsonDocumentAsync(
            marketMessage,
            cancellationToken)
            .ConfigureAwait(false);
        if (document == null)
        {
            return new IncomingMarketMessageParserResult(
                InvalidMessageStructure.From("Failed to parse JSON document."));
        }

        var validationErrors = ValidateAndParse(
            document,
            schemaResult,
            out var header,
            out var series);
        if (validationErrors.Any())
            return new IncomingMarketMessageParserResult(validationErrors.ToArray());

        if (header is null)
            throw new NullReferenceException("Header cannot be null");

        if (series is null)
            throw new NullReferenceException("Series cannot be null");

        return CreateResult(header, series.AsReadOnly());
    }

    protected abstract IIncomingMessageSeries ParseTransaction(JsonElement transactionElement, string senderNumber);

    protected override async Task<(JsonSchema? Schema, ValidationError? ValidationError)> GetSchemaAsync(
        IIncomingMarketMessageStream marketMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(DocumentName);
        const string schemaVersion = "0";
        var jsonSchema = await _schemaProvider.GetSchemaAsync<JsonSchema>(
                DocumentName.ToUpper(CultureInfo.InvariantCulture),
                schemaVersion,
                cancellationToken)
            .ConfigureAwait(false);
        if (jsonSchema is null)
        {
            return (jsonSchema, new InvalidBusinessReasonOrVersion(DocumentName, schemaVersion));
        }

        return (jsonSchema, null);
    }

    protected abstract IncomingMarketMessageParserResult CreateResult(
        MessageHeader header,
        IReadOnlyCollection<IIncomingMessageSeries> transactions);

    private static JsonElement GetPropertyWithValue(JsonElement headerElement, string elementName)
    {
        return headerElement.GetProperty(elementName).GetProperty(ValueElementName);
    }

    private IReadOnlyCollection<ValidationError> ValidateAndParse(
        JsonDocument document,
        JsonSchema schemaResult,
        out MessageHeader? header,
        out IList<IIncomingMessageSeries>? series)
    {
        header = null;
        series = new List<IIncomingMessageSeries>();

        var validationError = GetHeaderNode(document, out var headerNode);
        if (validationError is not null)
            return [validationError];

        if (headerNode is null)
            throw new NullReferenceException("Header cannot be null");

        var transactionElements = document.RootElement
            .GetProperty(HeaderElementName)
            .GetProperty(SeriesElementName);
        foreach (var transactionElement in transactionElements.EnumerateArray())
        {
            // This validates a full document that contains only a single transaction.
            // By isolating one transaction per validation, we reduce peak memory usage
            // and allow memory to be reused between validations.
            var documentWithOneTransaction = GetDocumentWithOneSeriesElement(transactionElement, headerNode);
            if (!IsValid(documentWithOneTransaction, schemaResult, out var validationErrors))
                return validationErrors;

            header ??= ParseHeader(document);
            series.Add(ParseTransaction(transactionElement, header.SenderId));
        }

        return [];
    }

    private ValidationError? GetHeaderNode(JsonDocument document, out JsonNode? headerElement)
    {
        var root = document.RootElement.AsNode();
        headerElement = root?[HeaderElementName];
        if (headerElement == null)
            return InvalidMessageStructure.From($"Could not find {HeaderElementName} in the document");
        return null;
    }

    private async Task<JsonDocument?> TryParseJsonDocumentAsync(
        IIncomingMarketMessageStream marketMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var document = await JsonDocument.ParseAsync(marketMessage.Stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return document;
        }
        catch (JsonException exception)
        {
            marketMessage.Stream.Seek(0, SeekOrigin.Begin); 
            using var reader = new StreamReader(marketMessage.Stream);
            var buffer = new char[1500];
            var charsRead = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            var first1500Chars = new string(buffer, 0, charsRead);

            _logger.LogError(exception, "Failed to parse JSON document. First 1500 chars: {First1500Chars}", first1500Chars);
            return null;
        }
    }

    private JsonNode? GetDocumentWithOneSeriesElement(
        JsonElement seriesElement,
        JsonNode headerElement)
    {
        JsonDocument? documentWithOneTransaction = null;
        try
        {
            headerElement[SeriesElementName] = new JsonArray(seriesElement.AsNode());
            return headerElement.Parent;
        }
        catch
        {
            documentWithOneTransaction?.Dispose();
            return null;
        }
    }

    private MessageHeader ParseHeader(JsonDocument document)
    {
        var headerElement = document.RootElement.GetProperty(HeaderElementName);
        var businessType = headerElement
            .TryGetProperty(BusinessSectorTypeElementName, out var property)
            ? property.GetProperty(ValueElementName).GetString()
            : null;

        return new MessageHeader(
            headerElement.GetProperty(IdentificationElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, MessageTypeElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, ProcessTypeElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, SenderIdentificationElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, SenderRoleElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, ReceiverIdentificationElementName).GetString() ?? string.Empty,
            GetPropertyWithValue(headerElement, ReceiverRoleElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(CreatedDateElementName).GetString() ?? string.Empty,
            businessType);
    }

    private bool IsValid(JsonNode? jsonDocument, JsonSchema schema, out IReadOnlyList<ValidationError> validationErrors)
    {
        if (jsonDocument is null)
        {
            validationErrors = [InvalidMessageStructure.From("Document is empty")];
            return false;
        }

        var result = schema.Evaluate(jsonDocument, _cachedEvaluationOptions);
        if (!result.IsValid)
        {
            validationErrors = FindErrorsForInvalidEvaluation(result);
            return false;
        }

        validationErrors = new List<ValidationError>();
        return true;
    }

    private IReadOnlyList<ValidationError> FindErrorsForInvalidEvaluation(EvaluationResults result)
    {
        var validationErrors = new List<ValidationError>();
        if (!result.IsValid && result.Errors != null)
        {
            var propertyName = result.InstanceLocation.ToString();
            foreach (var error in result.Errors)
            {
                validationErrors.Add(InvalidMessageStructure.From($"{propertyName}: {error}"));
            }
        }

        foreach (var detail in result.Details)
        {
            validationErrors.AddRange(FindErrorsForInvalidEvaluation(detail));
        }

        return validationErrors;
    }
}
