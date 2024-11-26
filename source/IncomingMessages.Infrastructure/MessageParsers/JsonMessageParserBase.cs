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

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Json.Schema;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;

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

    protected abstract string HeaderElementName { get; }

    protected abstract string DocumentName { get; }

    private Collection<ValidationError> ValidationErrors { get; } = [];

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
            return new IncomingMarketMessageParserResult(ValidationErrors.ToArray());
        }

        if (IsValid(document, schemaResult) == false)
        {
            return new IncomingMarketMessageParserResult(ValidationErrors.ToArray());
        }

        var header = ParseHeader(document);
        var transactionElements = document.RootElement.GetProperty(HeaderElementName).GetProperty(SeriesElementName);
        var transactions = new List<IIncomingMessageSeries>();

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
        var schemaVersion = "0";
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
        return element.ToString().Trim('"');
    }

    private static string? GetBusinessType(JsonElement element)
    {
        return element.TryGetProperty("businessSector.type", out var property) ? property.GetProperty("value").ToString() : null;
    }

    private MessageHeader ParseHeader(JsonDocument document)
    {
        var headerElement = document.RootElement.GetProperty(HeaderElementName);
        return new MessageHeader(
            headerElement.GetProperty(IdentificationElementName).ToString(),
            headerElement.GetProperty(MessageTypeElementName).GetProperty(ValueElementName).ToString(),
            headerElement.GetProperty(ProcessTypeElementName).GetProperty(ValueElementName).ToString(),
            headerElement.GetProperty(SenderIdentificationElementName).GetProperty(ValueElementName).ToString(),
            headerElement.GetProperty(SenderRoleElementName).GetProperty(ValueElementName).ToString(),
            headerElement.GetProperty(ReceiverIdentificationElementName).GetProperty(ValueElementName).ToString(),
            headerElement.GetProperty(ReceiverRoleElementName).GetProperty(ValueElementName).ToString(),
            GetJsonDateStringWithoutQuotes(headerElement.GetProperty(CreatedDateElementName)),
            GetBusinessType(headerElement));
    }

    private bool IsValid(JsonDocument jsonDocument, JsonSchema schema)
    {
        var result = schema.Evaluate(jsonDocument, new EvaluationOptions() { OutputFormat = OutputFormat.Hierarchical, });
        if (result.IsValid == false)
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
        ValidationErrors.Add(InvalidMessageStructure.From(errorMessage));
    }
}
