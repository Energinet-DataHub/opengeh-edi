﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization.Converters;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using NodaTime.Serialization.SystemTextJson;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;

/// <summary>
/// JSON serializer that specifically support NodaTime's <see cref="NodaTime.Instant"/>.
/// </summary>
public sealed class Serializer : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public Serializer(JavaScriptEncoder encoder)
    {
        _options = new JsonSerializerOptions { Encoder = encoder, PropertyNameCaseInsensitive = true };

        _options.Converters.Add(NodaConverters.InstantConverter);
        _options.Converters.Add(new CustomJsonConverterForType());
        _options.Converters.Add(new ObjectToInferredTypesConverter());
    }

    public Serializer()
    {
        _options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(
                UnicodeRanges.BasicLatin,
                UnicodeRanges.Latin1Supplement,
                UnicodeRanges.LatinExtendedA),
            PropertyNameCaseInsensitive = true,
        };

        _options.Converters.Add(NodaConverters.InstantConverter);
        _options.Converters.Add(new CustomJsonConverterForType());
        _options.Converters.Add(new ObjectToInferredTypesConverter());
    }

    public ValueTask<TValue> DeserializeAsync<TValue>(Stream json, CancellationToken cancellationToken)
    {
        return JsonSerializer.DeserializeAsync<TValue>(json, _options, cancellationToken)!;
    }

    public TValue Deserialize<TValue>(string json)
    {
        return JsonSerializer.Deserialize<TValue>(json, _options)!;
    }

    public object Deserialize(string json, Type returnType)
    {
        return JsonSerializer.Deserialize(json, returnType, _options)!;
    }

    public string Serialize<TValue>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize<object>(value, _options);
    }

    public Task SerializeAsync<TValue>(Stream stream, TValue value)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.SerializeAsync(stream, value, _options);
    }
}
