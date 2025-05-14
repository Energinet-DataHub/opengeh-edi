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
    [JsonConstructor]
    public ExternalId(string value)
    {
        Value = value;
    }

    public ExternalId(Guid value)
        : this(value.ToString())
    { }

    public string Value { get; }

    public static ExternalId New() => new(Guid.NewGuid().ToString());
}
