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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// An ExternalId to represent what the systems external source uses as an identifier
/// The value provided must not change even if operation fails.
/// This is to ensure that the same external id is used for the same operation.
/// So a message can only be delivered once for a given external id. (Unless additional logic is provided to check for idempotency)
/// </summary>
[Serializable]
public class ExternalId
{
    /// <summary>
    /// Must fit the BINARY(16) ExternalId column in the OutgoingMessages table.
    /// </summary>
    private const int BinaryMaxLength = 16;

    [JsonConstructor]
    public ExternalId(byte[] value)
    {
        if (value.Length > BinaryMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value.Length,
                $"ExternalId (string value: \"{Encoding.UTF8.GetString(value)}\", guid value: \"{new Guid(value)}\") must be {BinaryMaxLength} bytes or less");
        }

        Value = value;
    }

    public ExternalId(Guid value)
        : this(value.ToByteArray())
    { }

    /// <summary>
    /// Store the external id as a binary. This is done to increase performance, instead of using VARCHAR(36) or similar.
    /// </summary>
    public byte[] Value { get; }

    public static ExternalId New() => new(Guid.NewGuid());

    /// <summary>
    /// Hashes the given <paramref name="values"/> input to create a string, and ensures that the length is less than
    /// or equal to the <see cref="BinaryMaxLength"/>.
    /// </summary>
    /// <remarks>
    /// The hashing algorithm used is SHA256, which has a low chance of collision. The hashing algorithm ensures
    /// the "avalanche effect", which means that even if the input only changes by 1 bit, the output will be completely
    /// different.
    /// </remarks>
    /// <returns>An <see cref="ExternalId"/> that contains a hashed value. The output will be the same given that the input is the same.</returns>
    public static ExternalId HashValuesWithMaxLength(params string[] values)
    {
        if (values.Length == 0)
            throw new ArgumentException("At least one value must be provided.", nameof(values));

        // Combine all values into a single string.
        var combinedInputString = string.Concat(values);

        // Hash the combined input string using SHA256 (low collision chance).
        var inputAsHash = SHA256.HashData(Encoding.UTF8.GetBytes(combinedInputString));

        // Truncate the hash to 16 bytes.
        // This means the entropy is now reduced to 2^128 (128 bits), which is still an astronomically large number,
        // so the chance of collision is negligible in practice. We would need to generate over 20 quintillion values
        // to reach a 50% chance of a collision (the birthday paradox).
        var truncatedHash = new byte[BinaryMaxLength];
        Array.Copy(
            sourceArray: inputAsHash,
            destinationArray: truncatedHash,
            length: BinaryMaxLength);

        return new ExternalId(truncatedHash);
    }
}
