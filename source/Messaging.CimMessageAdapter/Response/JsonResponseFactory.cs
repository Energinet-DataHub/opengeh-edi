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
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Messaging.CimMessageAdapter.Response;

public class JsonResponseFactory : IResponseFactory
{
    public ResponseMessage From(Result result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        return result.Success ? new ResponseMessage() : new ResponseMessage(CreateMessageBodyFrom(result));
    }

    protected static string CreateMessageBodyFrom(Result result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        var messageBody = new StringBuilder();
        var stringWriter = new StringWriter(messageBody);

        using var writer = new JsonTextWriter(stringWriter);

        writer.WriteStartObject();
        writer.WritePropertyName("Error");
        writer.WriteStartObject();
        writer.WritePropertyName("Code");
        writer.WriteValue(result.Errors.Count == 1 ? result.Errors.First().Code : "BadRequest");
        writer.WritePropertyName("Message");
        writer.WriteValue(result.Errors.Count == 1 ? result.Errors.First().Message : "Multiple errors in message");

        if (result.Errors.Count > 1)
        {
            writer.WritePropertyName("Details");
            writer.WriteStartObject();
            writer.WritePropertyName("Errors");
            writer.WriteStartArray();
            foreach (var validationError in result.Errors)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Code");
                writer.WriteValue(validationError.Code);
                writer.WritePropertyName("Message");
                writer.WriteValue(validationError.Message);
                writer.WriteEndObject();
            }

            writer.WriteEnd();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Close();

        return messageBody.ToString();
    }
}
