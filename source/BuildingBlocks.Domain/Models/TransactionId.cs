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
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
///     Represents a transaction id which used in communication between EDI and actors.
/// </summary>
[Serializable]
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
        return new TransactionId(
            Guid.NewGuid().ToString().Replace("-", string.Empty, StringComparison.InvariantCultureIgnoreCase));
    }
}
