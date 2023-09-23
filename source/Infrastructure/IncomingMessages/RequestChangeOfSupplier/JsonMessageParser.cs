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
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.BaseParsers;
using Energinet.DataHub.EDI.MarketTransactions;
using DocumentFormat = Energinet.DataHub.EDI.Domain.Documents.DocumentFormat;

namespace Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestChangeOfSupplier;

public class JsonMessageParser : JsonParserBase<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>,
    IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private const string DocumentName = "RequestChangeOfSupplier";

    public JsonMessageParser(JsonSchemaProvider schemaProvider)
        : base(schemaProvider)
    {
    }

    public DocumentFormat HandledFormat => DocumentFormat.Json;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>> ParseAsync(
        Stream message, CancellationToken cancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var schema = await GetSchemaAsync(DocumentName, cancellationToken).ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>(
                new InvalidBusinessReasonOrVersion(DocumentName, "0"));
        }

        ResetMessagePosition(message);

        var errors = await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>(errors.ToArray());
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

    private MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand> ParseJsonData(
        MessageHeader header,
        JsonElement marketActivityRecordJson)
    {
        var marketActivityRecords = new List<MarketActivityRecord>();

        foreach (var jsonElement in marketActivityRecordJson.EnumerateArray())
        {
            marketActivityRecords.Add(MarketActivityRecordFrom(jsonElement));
        }

        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>(
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
