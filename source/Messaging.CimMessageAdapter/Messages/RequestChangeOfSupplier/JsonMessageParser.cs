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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Json.Schema;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Errors;
using Messaging.Domain.OutgoingMessages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;

public class JsonMessageParser : IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>
{
    private const string MarketActivityRecordElementName = "MktActivityRecord";
    private const string HeaderElementName = "RequestChangeOfSupplier_MarketDocument";
    private readonly ISchemaProvider _schemaProvider;
    private readonly List<ValidationError> _errors = new();

    public JsonMessageParser()
    {
        _schemaProvider = new JsonSchemaProvider();
    }

    public CimFormat HandledFormat => CimFormat.Json;

    public async Task<MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>> ParseAsync(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string processType;
        try
        {
            processType = GetBusinessProcessType(message);
        }
        catch (JsonException exception)
        {
            return InvalidJsonFailure(exception);
        }

        var schema = await _schemaProvider.GetSchemaAsync<JsonSchema>(processType, "0").ConfigureAwait(false);
        if (schema is null)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(new UnknownBusinessProcessTypeOrVersion(processType, "0"));
        }

        ResetMessagePosition(message);

        await ValidateMessageAsync(schema, message).ConfigureAwait(false);

        if (_errors.Count > 0)
        {
            return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(_errors.ToArray());
        }

        var streamReader = new StreamReader(message, leaveOpen: true);
        try
        {
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                try
                {
                    return ParseJsonData(jsonTextReader);
                }
                catch (JsonException exception)
                {
                    return InvalidJsonFailure(exception);
                }
                catch (ArgumentException argumentException)
                {
                    return InvalidJsonFailure(argumentException);
                }
            }
        }
        catch (IOException e)
        {
            return InvalidJsonFailure(e);
        }
        finally
        {
            streamReader.Dispose();
        }
    }

    private static string[] SplitNamespace(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        string[] split;
        ResetMessagePosition(message);
        var streamReader = new StreamReader(message, leaveOpen: true);
        using (var jsonTextReader = new JsonTextReader(streamReader))
        {
            var serializer = new JsonSerializer();
            var deserialized = (JObject)serializer.Deserialize(jsonTextReader);
            var path = deserialized.First.Path;
            split = path.Split('_');
        }

        return split;
    }

    private static string GetBusinessProcessType(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var processType = split[0];
        return processType;
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> ParseJsonData(JsonTextReader jsonTextReader)
    {
        var marketActivityRecords = new List<MarketActivityRecord>();
        var serializer = new JsonSerializer();
        var jsonRequest = serializer.Deserialize<JObject>(jsonTextReader);
        var headerToken = jsonRequest.SelectToken(HeaderElementName);
        var messageHeader = MessageHeaderFrom(headerToken);
        marketActivityRecords.AddRange(headerToken[MarketActivityRecordElementName].Select(MarketActivityRecordFrom));

        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(new RequestChangeOfSupplierParsedMessage(messageHeader, marketActivityRecords));
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction> InvalidJsonFailure(Exception exception)
    {
        return new MessageParserResult<MarketActivityRecord, RequestChangeOfSupplierTransaction>(InvalidMessageStructure.From(exception));
    }

    private static MessageHeader MessageHeaderFrom(JToken token)
    {
        return new MessageHeader(
            token["mRID"].ToString(),
            token["process.processType"]["value"].ToString(),
            token["sender_MarketParticipant.mRID"]["value"].ToString(),
            token["sender_MarketParticipant.marketRole.type"]["value"].ToString(),
            token["receiver_MarketParticipant.mRID"]["value"].ToString(),
            token["receiver_MarketParticipant.marketRole.type"]["value"].ToString(),
            token["createdDateTime"].ToString());
    }

    private static MarketActivityRecord MarketActivityRecordFrom(JToken token)
    {
        return new MarketActivityRecord()
        {
            Id = token["mRID"].ToString(),
            ConsumerId = token["marketEvaluationPoint.customer_MarketParticipant.mRID"]["value"].ToString(),
            BalanceResponsibleId = token["marketEvaluationPoint.balanceResponsibleParty_MarketParticipant.mRID"]["value"].ToString(),
            EnergySupplierId = token["marketEvaluationPoint.energySupplier_MarketParticipant.mRID"]["value"].ToString(),
            MarketEvaluationPointId = token["marketEvaluationPoint.mRID"]["value"].ToString(),
            ConsumerName = token["marketEvaluationPoint.customer_MarketParticipant.name"].ToString(),
            EffectiveDate = token["start_DateAndOrTime.dateTime"].ToString(),
        };
    }

    private async Task ValidateMessageAsync(JsonSchema schema, Stream message)
    {
        var jsonDocument = await JsonDocument.ParseAsync(message).ConfigureAwait(false);

        var validationOptions = new ValidationOptions()
        {
            OutputFormat = OutputFormat.Detailed,
        };

        var validationResult = schema.Validate(jsonDocument, validationOptions);

        if (!validationResult.IsValid)
        {
            AddValidationErrors(validationResult);
        }

        ResetMessagePosition(message);
    }

    private void AddValidationErrors(ValidationResults validationResult)
    {
        AddValidationError(validationResult.Message);

        if (validationResult.HasNestedResults)
        {
            foreach (var result in validationResult.NestedResults)
            {
                AddValidationError(result.Message);
            }
        }
    }

    private void AddValidationError(string? errorMessage)
    {
        if (errorMessage != null)
        {
            _errors.Add(InvalidMessageStructure.From(errorMessage));
        }
    }
}
