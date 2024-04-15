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

using System.Collections.Generic;
using System.Text.Json;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Json;

internal static class JsonWriterHelper
{
    internal static void WriteObject(this Utf8JsonWriter writer, string name, params KeyValuePair<string, string>[] values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();

        foreach (var value in values)
        {
            writer.WritePropertyName(value.Key);
            writer.WriteStringValue(value.Value);
        }

        writer.WriteEndObject();
    }

    internal static void WriteObject(this Utf8JsonWriter writer, string name, params KeyValuePair<string, int>[] values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();

        foreach (var value in values)
        {
            writer.WritePropertyName(value.Key);
            writer.WriteNumberValue(value.Value);
        }

        writer.WriteEndObject();
    }

    internal static void WriteObject(this Utf8JsonWriter writer, string name, params KeyValuePair<string, decimal>[] values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();

        foreach (var value in values)
        {
            writer.WritePropertyName(value.Key);
            writer.WriteNumberValue(value.Value);
        }

        writer.WriteEndObject();
    }

    internal static void WriteProperty(this Utf8JsonWriter writer, string name, string value)
    {
        writer.WritePropertyName(name);
        writer.WriteStringValue(value);
    }

    internal static void WriteProperty(this Utf8JsonWriter writer, string name, decimal value)
    {
        writer.WritePropertyName(name);
        writer.WriteNumberValue(value);
    }
}
