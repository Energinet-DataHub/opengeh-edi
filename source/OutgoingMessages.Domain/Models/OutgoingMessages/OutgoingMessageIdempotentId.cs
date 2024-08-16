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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

/// <summary>
/// A hashed value for an outgoing message to ensure idempotency
/// </summary>
public record OutgoingMessageIdempotentId
{
    private OutgoingMessageIdempotentId(byte[] value)
    {
        Value = value;
    }

    public byte[] Value { get; }

    public static OutgoingMessageIdempotentId New(params string[] values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one value must be provided", nameof(values));
        }

        var concatenatedValues = string.Join(string.Empty, values);
        using var hash = SHA256.Create();
        var hashed = hash.ComputeHash(Encoding.UTF8.GetBytes(concatenatedValues));

        return new OutgoingMessageIdempotentId(hashed);
    }

    public static OutgoingMessageIdempotentId CreateFromExisting(byte[] existingOutgoingMessageIdempotencyId)
    {
        return new OutgoingMessageIdempotentId(existingOutgoingMessageIdempotencyId);
    }
}
