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

using System.Text;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;

public class JsonResponseFactory : IResponseFactory
{
#pragma warning disable CA1822
    public DocumentFormat HandledFormat => DocumentFormat.Json;
#pragma warning restore CA1822

    public ResponseMessage From(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Success ? new ResponseMessage() : new ResponseMessage(CreateMessageBodyFrom(result));
    }

    private static string CreateMessageBodyFrom(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var messageBody = new MemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(messageBody, options);

        writer.WriteStartObject();
        writer.WritePropertyName("Error");
        writer.WriteStartObject();
        writer.WritePropertyName("Code");
        writer.WriteStringValue(result.Errors.Count == 1 ? result.Errors.First().Code : "BadRequest");
        writer.WritePropertyName("Message");
        writer.WriteStringValue(result.Errors.Count == 1 ? result.Errors.First().Message : "Multiple errors in message");
        writer.WritePropertyName("Target");
        writer.WriteStringValue(result.Errors.Count == 1 ? result.Errors.First().Target : string.Empty);

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
                writer.WriteStringValue(validationError.Code);
                writer.WritePropertyName("Message");
                writer.WriteStringValue(validationError.Message);
                writer.WritePropertyName("Target");
                writer.WriteStringValue(validationError.Target);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(messageBody.ToArray());
    }
}
