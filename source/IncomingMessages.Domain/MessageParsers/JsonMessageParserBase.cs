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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

public abstract class JsonMessageParserBase(JsonSchemaProvider schemaProvider) : MessageParserBase<JsonSchema>()
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

    //JsonSchema.Net optimizes repeated evaluations with the same schema by performing some static analysis during the first evaluation.
    //https://docs.json-everything.net/schema/basics/#schema-options
    private static readonly EvaluationOptions _cachedEvaluationOptions = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
    };

    private readonly JsonSchemaProvider _schemaProvider = schemaProvider;

    private List<ValidationError> _validationErrors = [];

    protected abstract string HeaderElementName { get; }

    protected abstract string DocumentName { get; }

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        JsonSchema schemaResult,
        CancellationToken cancellationToken)
    {
        using var document = await TryParseJsonDocumentAsync(marketMessage, cancellationToken).ConfigureAwait(false);
        if (document == null)
            return new IncomingMarketMessageParserResult(_validationErrors.ToArray());

        var transactions = ValidateAndParseTransactions(document, schemaResult);
        if (transactions == null)
            return new IncomingMarketMessageParserResult(_validationErrors.ToArray());

        var header = ParseHeader(document);
        return CreateResult(header, transactions);
    }

    protected abstract IIncomingMessageSeries ParseTransaction(JsonElement transactionElement, string senderNumber);

    protected override async Task<(JsonSchema? Schema, ValidationError? ValidationError)> GetSchemaAsync(IIncomingMarketMessageStream marketMessage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(DocumentName);
        const string schemaVersion = "0";
        var jsonSchema = await _schemaProvider.GetSchemaAsync<JsonSchema>(DocumentName.ToUpper(CultureInfo.InvariantCulture), schemaVersion, cancellationToken).ConfigureAwait(false);
        if (jsonSchema is null)
        {
            return (jsonSchema, new InvalidBusinessReasonOrVersion(DocumentName, schemaVersion));
        }

        return (jsonSchema, null);
    }

    protected abstract IncomingMarketMessageParserResult CreateResult(MessageHeader header, IReadOnlyCollection<IIncomingMessageSeries> transactions);

    private IReadOnlyCollection<IIncomingMessageSeries>? ValidateAndParseTransactions(
        JsonDocument document,
        JsonSchema schemaResult)
    {
        var headerElement = GetHeaderElement(document);
        if (headerElement == null)
            return null;

        var transactionElements = document.RootElement.GetProperty(HeaderElementName).GetProperty(SeriesElementName);
        var transactions = new List<IIncomingMessageSeries>(transactionElements.GetArrayLength());
        var senderId = headerElement[SenderIdentificationElementName]?[ValueElementName]?.GetValue<string>() ?? string.Empty;
        foreach (var transactionElement in transactionElements.EnumerateArray())
        {
            // This validates a full document that contains only a single transaction.
            // By isolating one transaction per validation, we reduce peak memory usage
            // and allow memory to be reused between validations.
            var documentWithOneTransaction = GetDocumentWithOneSeriesElement(transactionElement, headerElement);
            if (!IsValid(documentWithOneTransaction, schemaResult))
                return null;

            transactions.Add(ParseTransaction(transactionElement, senderId));
        }

        return transactions;
    }

    private JsonNode? GetHeaderElement(JsonDocument document)
    {
        var root = JsonNode.Parse(document.RootElement.GetRawText());
        var headerElement = root?[HeaderElementName];
        if (headerElement == null)
            AddValidationError($"Could not find {HeaderElementName} in the document");
        return headerElement;
    }

    private async Task<JsonDocument?> TryParseJsonDocumentAsync(
        IIncomingMarketMessageStream marketMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            return await JsonDocument.ParseAsync(marketMessage.Stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            AddValidationError(exception.Message);
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
        var businessType = headerElement.TryGetProperty(BusinessSectorTypeElementName, out var property)
            ? property.GetProperty(ValueElementName).GetString()
            : null;

        return new MessageHeader(
            headerElement.GetProperty(IdentificationElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(MessageTypeElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ProcessTypeElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(SenderIdentificationElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(SenderRoleElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ReceiverIdentificationElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ReceiverRoleElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(CreatedDateElementName).GetString() ?? string.Empty,
            businessType);
    }

    private bool IsValid(JsonNode? jsonDocument, JsonSchema schema)
    {
        if (jsonDocument is null)
        {
            AddValidationError("Document is empty");
            return false;
        }

        var result = schema.Evaluate(jsonDocument, _cachedEvaluationOptions);
        if (!result.IsValid)
        {
            FindErrorsForInvalidEvaluation(result);
        }

        return result.IsValid;
    }

    private void FindErrorsForInvalidEvaluation(EvaluationResults result)
    {
        if (!result.IsValid && result.Errors != null)
        {
            var propertyName = result.InstanceLocation.ToString();
            foreach (var error in result.Errors)
            {
                AddValidationError($"{propertyName}: {error}");
            }
        }

        foreach (var detail in result.Details)
        {
            FindErrorsForInvalidEvaluation(detail);
        }
    }

    private void AddValidationError(string errorMessage)
    {
        _validationErrors ??= new List<ValidationError>();
        _validationErrors.Add(InvalidMessageStructure.From(errorMessage));
    }
}
