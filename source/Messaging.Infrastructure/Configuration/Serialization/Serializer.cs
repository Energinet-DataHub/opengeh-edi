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
using System.Text.Json;
using System.Threading.Tasks;
using Messaging.Infrastructure.Configuration.Serialization.Converters;
using NodaTime.Serialization.SystemTextJson;

namespace Messaging.Infrastructure.Configuration.Serialization
{
    /// <summary>
    /// JSON serializer that specifically support NodaTime's <see cref="NodaTime.Instant"/>.
    /// </summary>
    public class Serializer : ISerializer
    {
        private readonly JsonSerializerOptions _options;

        public Serializer()
        {
            _options = new JsonSerializerOptions();
            _options.PropertyNameCaseInsensitive = true;
            _options.Converters.Add(NodaConverters.InstantConverter);
            _options.Converters.Add(new CustomJsonConverterForType());
            _options.Converters.Add(new ObjectToInferredTypesConverter());
        }

        public ValueTask<object> DeserializeAsync(Stream utf8Json, Type returnType)
        {
            return System.Text.Json.JsonSerializer.DeserializeAsync(utf8Json, returnType, _options)!;
        }

        public TValue Deserialize<TValue>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<TValue>(json, _options)!;
        }

        public object Deserialize(string json, Type returnType)
        {
            return System.Text.Json.JsonSerializer.Deserialize(json, returnType, _options)!;
        }

        public string Serialize<TValue>(TValue value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return System.Text.Json.JsonSerializer.Serialize<object>(value, _options);
        }

        public Task SerializeAsync<TValue>(Stream stream, TValue value)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return System.Text.Json.JsonSerializer.SerializeAsync(stream, value, _options);
        }
    }
}
