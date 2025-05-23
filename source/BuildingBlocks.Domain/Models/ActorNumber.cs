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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Exceptions;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

[Serializable]
public class ActorNumber : ValueObject
{
    [JsonConstructor]
    private ActorNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ActorNumber Create(ProcessManager.Abstractions.Core.ValueObjects.ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        return TryCreate(actorNumber.Value) ?? throw InvalidActorNumberException.Create(actorNumber.Value);
    }

    public static ActorNumber Create(string actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        return TryCreate(actorNumber) ?? throw InvalidActorNumberException.Create(actorNumber);
    }

    public static ActorNumber? TryCreate(string? actorNumber)
    {
        if (actorNumber == null)
            return null;

        return IsGlnNumber(actorNumber) || IsEic(actorNumber)
            ? new ActorNumber(actorNumber)
            : null;
    }

    public bool IsEic()
    {
        return IsEic(Value);
    }

    public bool IsGlnNumber()
    {
        return IsGlnNumber(Value);
    }

    public ProcessManager.Abstractions.Core.ValueObjects.ActorNumber ToProcessManagerActorNumber()
    {
        return ProcessManager.Abstractions.Core.ValueObjects.ActorNumber.Create(Value);
    }

    private static bool IsEic(string actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        return actorNumber.Length == 16;
    }

    private static bool IsGlnNumber(string actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        return actorNumber.Length == 13 && long.TryParse(actorNumber, out _);
    }
}
