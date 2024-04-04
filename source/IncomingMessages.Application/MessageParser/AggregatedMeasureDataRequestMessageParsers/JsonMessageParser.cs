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
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Application.Factories;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers;

public class JsonMessageParser : JsonParserBase, IMessageParser
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestAggregatedMeasureData_MarketDocument";
    private const string DocumentName = "RequestAggregatedMeasureData";

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.RequestAggregatedMeasureData;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(
        IIncomingMessageStream incomingMessageStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessageStream);

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);
        if (schema is null)
        {
            return new IncomingMarketMessageParserResult(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        try
        {
            var errors = await ValidateMessageAsync(schema, incomingMessageStream).ConfigureAwait(false);

            if (errors.Count > 0)
            {
                return new IncomingMarketMessageParserResult(errors.ToArray());
            }

            using var document = await JsonDocument.ParseAsync(incomingMessageStream.Stream, cancellationToken: cancellationToken)
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
