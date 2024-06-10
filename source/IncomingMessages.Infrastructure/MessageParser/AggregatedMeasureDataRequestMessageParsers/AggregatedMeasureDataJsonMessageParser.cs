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
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.Factories;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.AggregatedMeasureDataRequestMessageParsers;

public class AggregatedMeasureDataJsonMessageParser : JsonParserBase, IMarketMessageParser
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private const string DocumentName = "RequestAggregatedMeasureData";

    public AggregatedMeasureDataJsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMarketMessageStream incomingMarketMessageStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMarketMessageStream);

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);
        if (schema is null)
        {
            return new IncomingMarketMessageParserResult(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        try
        {
            var errors = await ValidateMessageAsync(schema, incomingMarketMessageStream).ConfigureAwait(false);

            if (errors.Count > 0)
            {
                return new IncomingMarketMessageParserResult(errors.ToArray());
            }

            using var document = await JsonDocument.ParseAsync(incomingMarketMessageStream.Stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var header = document.RootElement.GetProperty(HeaderElementName);
            var seriesJson = header.GetProperty(SeriesElementName);

            return ParseJsonData(MessageHeaderFrom(header), seriesJson);
        }
        catch (JsonException exception)
        {
            return InvalidJsonFailure(exception);
        }
        catch (ArgumentException argumentException)
        {
            return InvalidJsonFailure(argumentException);
        }
        catch (IOException e)
        {
            return InvalidJsonFailure(e);
        }
    }

    private static RequestAggregatedMeasureDataMessageSeries SeriesFrom(JsonElement element)
    {
        return new RequestAggregatedMeasureDataMessageSeries(
            element.GetProperty("mRID").ToString(),
            GetPropertyWithValue(element, "marketEvaluationPoint.type"),
            GetPropertyWithValue(element, "marketEvaluationPoint.settlementMethod"),
            element.GetProperty("start_DateAndOrTime.dateTime").ToString(),
            element.TryGetProperty("end_DateAndOrTime.dateTime", out var endDateProperty) ? endDateProperty.ToString() : null,
            GetPropertyWithValue(element, "meteringGridArea_Domain.mRID"),
            GetPropertyWithValue(element, "energySupplier_MarketParticipant.mRID"),
            GetPropertyWithValue(element, "balanceResponsibleParty_MarketParticipant.mRID"),
            GetPropertyWithValue(element, "settlement_Series.version"));
    }

    private static string? GetPropertyWithValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetProperty("value").ToString() : null;
    }

    private static IncomingMarketMessageParserResult ParseJsonData(
        MessageHeader header,
        JsonElement seriesJson)
    {
        var series = new List<RequestAggregatedMeasureDataMessageSeries>();

        foreach (var jsonElement in seriesJson.EnumerateArray())
        {
            series.Add(SeriesFrom(jsonElement));
        }

        return new IncomingMarketMessageParserResult(
            RequestAggregatedMeasureDataMessageFactory.Create(header, series.AsReadOnly()));
    }
}
