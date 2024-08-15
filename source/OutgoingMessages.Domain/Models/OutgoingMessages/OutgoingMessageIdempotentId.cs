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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

/// <summary>
/// A hashed value for an outgoing message to ensure idempotency
/// </summary>
public record OutgoingMessageIdempotentId
{
    private OutgoingMessageIdempotentId(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static OutgoingMessageIdempotentId New(params string[] values)
    {
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one value must be provided", nameof(values));
        }

        var concatenatedValues = string.Join(string.Empty, values);
        return new OutgoingMessageIdempotentId(concatenatedValues.GetHashCode());
    }

    public static OutgoingMessageIdempotentId CreateFromExisting(int existingOutgoingMessageIdempotencyId)
    {
        return new OutgoingMessageIdempotentId(existingOutgoingMessageIdempotencyId);
    }
}
