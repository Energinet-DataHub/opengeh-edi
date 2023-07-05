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
using Application.IncomingMessages.RequestChangeOfSupplier;
using CimMessageAdapter.Messages;
using CimMessageAdapter.ValidationErrors;
using DocumentValidation;
using Infrastructure.IncomingMessages.BaseParsers;
using DocumentFormat = Domain.Documents.DocumentFormat;
using MessageHeader = Application.IncomingMessages.MessageHeader;

namespace Infrastructure.IncomingMessages.RequestChangeOfSupplier;

public class JsonMessageParser : JsonParserBase<MarketActivityRecord, RequestChangeOfSupplierTransaction>,
    IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private const string DocumentName = "RequestChangeOfSupplier";

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>> ParseAsync(
        Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        var errors = await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(errors.ToArray());
        }

        try
        {
            using var document = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            JsonElement header = document.RootElement.GetProperty(HeaderElementName);
            JsonElement seriesJson = header.GetProperty(MarketActivityRecordElementName);

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

    private MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> ParseJsonData(
        MessageHeader header,
        JsonElement marketActivityRecordJson)
    {
        var marketActivityRecords = new List<MarketActivityRecord>();

        foreach (var jsonElement in marketActivityRecordJson.EnumerateArray())
        {
            marketActivityRecords.Add(MarketActivityRecordFrom(jsonElement));
        }

        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(
            new RequestChangeOfSupplierIncomingMarketDocument(header, marketActivityRecords));
    }

    private MarketActivityRecord MarketActivityRecordFrom(JsonElement element)
    {
        return new MarketActivityRecord()
        {
            Id = element.GetProperty("mRID").ToString(),
            ConsumerId =
                element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("value")
                    .ToString(),
            ConsumerIdType =
                element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.mRID").GetProperty("codingScheme")
                    .ToString(),
            BalanceResponsibleId =
                element.GetProperty("marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID")
                    .GetProperty("value").ToString(),
            EnergySupplierId =
                element.GetProperty("marketEvaluationPoint.energySupplier_MarketParticipant.mRID").GetProperty("value")
                    .ToString(),
            MarketEvaluationPointId = element.GetProperty("marketEvaluationPoint.mRID").GetProperty("value").ToString(),
            ConsumerName = element.GetProperty("marketEvaluationPoint.customer_MarketParticipant.name").ToString(),
            EffectiveDate = GetJsonDateStringWithoutQuotes(element.GetProperty("start_DateAndOrTime.dateTime")),
        };
    }
}
