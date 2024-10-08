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

using System.Text.Json;
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
///     Represents a transaction id which used in communication between EDI and actors.
/// </summary>
[Serializable]
[JsonConverter(typeof(TransactionIdJsonConverter))]
public class TransactionId : ValueObject
{
    [JsonConstructor]
    private TransactionId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static TransactionId From(string transactionId)
    {
        if (transactionId is null || transactionId.Length > 36)
        {
            throw InvalidTransactionIdException.Create(transactionId);
        }

        return new TransactionId(transactionId);
    }

    public static TransactionId New()
    {
        // A normal UUID is 36 characters long, but unfortunately, the EBIX scheme only allows for 35 characters.
        // To make everyone happy---i.e. ensure unique ids (for most practical purposes anyway) and ensure
        // valid EBIX values---we'll just remove the dashes from the UUID.
        return new TransactionId(Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }
}

public class TransactionIdJsonConverter : JsonConverter<TransactionId>
{
    public override TransactionId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return TransactionId.From(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TransactionId value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteRawValue($"\"{value.Value}\"");
    }
}
