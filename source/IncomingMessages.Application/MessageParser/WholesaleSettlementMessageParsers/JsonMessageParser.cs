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
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.BaseParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.WholesaleSettlementMessageParsers;

public class JsonMessageParser : JsonParserBase, IMessageParser
{
    private const string SeriesElementName = "Series";
    private const string HeaderElementName = "RequestWholesaleSettlement_MarketDocument";
    private const string DocumentName = "RequestWholesaleSettlement";

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public IncomingDocumentType DocumentType => IncomingDocumentType.RequestWholesaleSettlement;

    public async Task<IncomingMarketMessageParserResult> ParseAsync(IIncomingMessageStream incomingMessageStream, CancellationToken cancellationToken)
    {
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

    private static IncomingMarketMessageParserResult ParseJsonData(
        MessageHeader header,
        JsonElement seriesJson)
    {
        var series = new List<RequestWholesaleServicesSeries>();

        foreach (var jsonElement in seriesJson.EnumerateArray())
        {
            series.Add(SeriesFrom(jsonElement));
        }

        return new IncomingMarketMessageParserResult(new RequestWholesaleServicesMessage(
            header.SenderId,
            header.SenderRole,
            header.ReceiverId,
            header.ReceiverRole,
            header.BusinessReason,
            header.MessageType,
            header.MessageId,
            header.CreatedAt,
            header.BusinessType,
            series.AsReadOnly()));
    }

    private static RequestWholesaleServicesSeries SeriesFrom(JsonElement element)
    {
        var chargeTypes = new List<RequestWholesaleServicesChargeType>();
        JsonElement? chargeTypeElements = element.TryGetProperty("ChargeType", out var chargeTypesElement)
            ? chargeTypesElement
            : null;
        if (chargeTypeElements != null)
        {
            foreach (var chargeTypeElement in chargeTypeElements.Value.EnumerateArray())
            {
                chargeTypes.Add(new RequestWholesaleServicesChargeType(
                    chargeTypeElement.TryGetProperty("mRID", out var id) ? id.ToString() : null,
                    GetPropertyWithValue(chargeTypeElement, "type")));
            }
        }

        return new RequestWholesaleServicesSeries(
            element.GetProperty("mRID").ToString(),
            element.GetProperty("start_DateAndOrTime.dateTime").ToString(),
            element.TryGetProperty("end_DateAndOrTime.dateTime", out var endDateProperty) ? endDateProperty.ToString() : null,
            GetPropertyWithValue(element, "meteringGridArea_Domain.mRID"),
            GetPropertyWithValue(element, "energySupplier_MarketParticipant.mRID"),
            GetPropertyWithValue(element, "settlement_Series.version"),
            element.TryGetProperty("aggregationSeries_Period.resolution", out var resolution) ? resolution.ToString() : null,
            GetPropertyWithValue(element, "chargeTypeOwner_MarketParticipant.mRID"),
            chargeTypes);
    }

    private static string? GetPropertyWithValue(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetProperty("value").ToString() : null;
    }
}
