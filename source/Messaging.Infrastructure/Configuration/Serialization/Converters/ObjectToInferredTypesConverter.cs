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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Messaging.Infrastructure.Configuration.Serialization.Converters
{
    public class ObjectToInferredTypesConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out var datetime) => datetime,
                JsonTokenType.String => reader.GetString() ?? string.Empty,
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone(),
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            object? value,
            JsonSerializerOptions options) =>
            System.Text.Json.JsonSerializer.Serialize(writer, value, value?.GetType() ?? throw new InvalidOperationException("Could not get runtime type"), options);
    }
}
