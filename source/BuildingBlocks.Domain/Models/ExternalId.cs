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
public record ExternalId
{
    private const int MaxLength = 36;

    [JsonConstructor]
    public ExternalId(string value)
    {
        if (value.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"ExternalId must be {MaxLength} characters or less");
        }

        Value = value;
    }

    public ExternalId(Guid value)
        : this(value.ToString())
    { }

    public string Value { get; }

    public static ExternalId New() => new(Guid.NewGuid());

    /// <summary>
    /// Hashes the given <paramref name="values"/> input to create a string, and ensures that the length is less than
    /// or equal to the <see cref="MaxLength"/>.
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
        var combinedInputString = string.Join(string.Empty, values);

        // Hash the combined input string using SHA256 (low collision chance).
        var inputAsHash = SHA256.HashData(Encoding.UTF8.GetBytes(combinedInputString));

        // Use standard Base64 and trim padding ('==' characters) if present.
        var asBase64String = Convert.ToBase64String(inputAsHash).TrimEnd('=');

        // Truncate to maximum MaxLength characters.
        var hashedValue = asBase64String.Length <= MaxLength
            ? asBase64String
            : asBase64String.Substring(0, MaxLength);

        return new ExternalId(hashedValue);
    }
}
