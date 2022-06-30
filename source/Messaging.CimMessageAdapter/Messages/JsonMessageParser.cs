using System;
using System.IO;
using System.Threading.Tasks;
using Messaging.Application.IncomingMessages;
using Messaging.Application.SchemaStore;
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

    public override Task<MessageParserResult> ParseAsync(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        throw new NotImplementedException();
    }

    protected override string[] SplitNamespace(Stream message)
    {
        using var streamReader = new StreamReader(message);
        using var jsonTextReader = new JsonTextReader(streamReader);
        var obj = JObject.Parse(jsonTextReader.ReadAsString());

        throw new NotImplementedException();
    }

    protected override string GetBusinessProcessType(Stream message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        var split = SplitNamespace(message);
        var processType = string.Join(string.Empty, split);
        return processType;
    }
}
