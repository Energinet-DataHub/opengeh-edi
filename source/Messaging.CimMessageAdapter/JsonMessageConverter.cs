using System;
using Messaging.Application.IncomingMessages;
using Messaging.CimMessageAdapter.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Messaging.CimMessageAdapter;

public class JsonMessageConverter : JsonConverter
{
    public override bool CanWrite => false;

    public override bool CanRead => true;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        var t = obj[0].Value<string>();

        return obj.ToObject<MessageHeader>(serializer);
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(JsonMessage);
    }
}
