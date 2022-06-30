using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Messaging.Application.IncomingMessages;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Errors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Messaging.CimMessageAdapter.Messages;

public class JsonMessageParser : MessageParser
{
    private readonly ISchemaProvider _schemaProvider;

    public JsonMessageParser(string? contentType)
    {
        _schemaProvider = SchemaProviderFactory.GetProvider(contentType);
    }

    public override async Task<MessageParserResult> ParseAsync(Stream message)
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
            return MessageParserResult.Failure(new UnknownBusinessProcessTypeOrVersion(processType, "0"));
        }

        ResetMessagePosition(message);
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

    protected override string[] SplitNamespace(Stream message)
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

    protected override string GetBusinessProcessType(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var processType = split[0];
        return processType;
    }

    private static MessageParserResult ParseJsonData(JsonTextReader jsonTextReader)
    {
        var serializer = new JsonSerializer();
        var deserialized = serializer.Deserialize<JObject>(jsonTextReader);  // <-----
        foreach (var pair in deserialized)
        {
            var value = pair.Value;
            var test = value["businessSector.type"]["value"];
            var jmessage = new JsonMessage
            {
                MessageId = value["mRID"].ToString(),
                CreatedAt = value["createdDateTime"].ToString(),
                ProcessType = value["process.processType"]["value"].ToString(),
                ReceiverId = value["receiver_MarketParticipant.mRID"]["value"].ToString(),
                ReceiverRole = value["receiver_MarketParticipant.marketRole.type"]["value"].ToString(),
                SenderId = value["sender_MarketParticipant.mRID"]["value"].ToString(),
                SenderRole = value["sender_MarketParticipant.marketRole.type"]["value"].ToString(),
            };
            var marketActivityRecords = new List<MktActivityRecord>();
            foreach (var record in value["MktActivityRecord"])
            {
                marketActivityRecords.Add(new MktActivityRecord
                {
                    Id = record["mRID"].ToString(),
                });
            }
        }

        throw new NotImplementedException();
    }

    private static void ResetMessagePosition(Stream message)
    {
        if (message.CanRead && message.Position > 0)
            message.Position = 0;
    }

    private static MessageParserResult InvalidJsonFailure(Exception exception)
    {
        return MessageParserResult.Failure(InvalidMessageStructure.From(exception));
    }
}
