﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
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
    private readonly JsonSchemaProvider _schemaProvider = schemaProvider;

    private List<ValidationError>? _validationErrors;

    protected abstract string HeaderElementName { get; }

    protected abstract string DocumentName { get; }

    protected override async Task<IncomingMarketMessageParserResult> ParseMessageAsync(
        IIncomingMarketMessageStream marketMessage,
        JsonSchema schemaResult,
        CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(marketMessage.Stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            AddValidationError(exception.Message);
            return new IncomingMarketMessageParserResult(_validationErrors!.ToArray());
        }

        if (!IsValid(document, schemaResult))
        {
            return new IncomingMarketMessageParserResult(_validationErrors!.ToArray());
        }

        var header = ParseHeader(document);
        var transactionElements = document.RootElement.GetProperty(HeaderElementName).GetProperty(SeriesElementName);
        var transactions = new List<IIncomingMessageSeries>(transactionElements.GetArrayLength());

        foreach (var transactionElement in transactionElements.EnumerateArray())
        {
            transactions.Add(ParseTransaction(transactionElement, header.SenderId));
        }

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

    private static string GetJsonDateStringWithoutQuotes(JsonElement element)
    {
        return element.GetString() ?? string.Empty;
    }

    private static string? GetBusinessType(JsonElement element)
    {
        return element.TryGetProperty("businessSector.type", out var property) ? property.GetProperty("value").GetString() : null;
    }

    private MessageHeader ParseHeader(JsonDocument document)
    {
        var headerElement = document.RootElement.GetProperty(HeaderElementName);
        return new MessageHeader(
            headerElement.GetProperty(IdentificationElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(MessageTypeElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ProcessTypeElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(SenderIdentificationElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(SenderRoleElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ReceiverIdentificationElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            headerElement.GetProperty(ReceiverRoleElementName).GetProperty(ValueElementName).GetString() ?? string.Empty,
            GetJsonDateStringWithoutQuotes(headerElement.GetProperty(CreatedDateElementName)),
            GetBusinessType(headerElement));
    }

    private bool IsValid(JsonDocument jsonDocument, JsonSchema schema)
    {
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
        if (!result.IsValid)
        {
            FindErrorsForInvalidEvaluation(result);
        }

        if (!jsonDocument.RootElement.EnumerateObject().Any())
        {
            AddValidationError("Document is empty");
            return false;
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
