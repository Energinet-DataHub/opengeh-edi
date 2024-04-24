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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.Process.Interfaces;

/// <summary>
/// The actor who a request/process is made on behalf of (the original actor is the actor who owns the data,
/// not necessarily the actor who made the request, in case of delegation).
/// </summary>
[Serializable]
public record OriginalActor
{
    [JsonConstructor]
    private OriginalActor(ActorNumber actorNumber, ActorRole actorRole)
    {
        ActorNumber = actorNumber;
        ActorRole = actorRole;
    }

    public ActorNumber ActorNumber { get; init; }

    public ActorRole ActorRole { get; init; }

    public static OriginalActor From(RequestedByActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return new OriginalActor(actor.ActorNumber, actor.ActorRole);
    }

    public static OriginalActor From(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return new OriginalActor(actor.ActorNumber, actor.ActorRole);
    }

    public static OriginalActor From(ActorNumber actorNumber, ActorRole actorRole)
    {
        return new OriginalActor(actorNumber, actorRole);
    }
}
