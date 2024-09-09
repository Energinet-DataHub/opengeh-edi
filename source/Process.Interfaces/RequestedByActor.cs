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

using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.Process.Interfaces;

/// <summary>
/// The actor who created the request/process. This is typically the one who owns the request/process, but in
/// case of delegation it could be someone else.
/// </summary>
[Serializable]
public record RequestedByActor
{
    [JsonConstructor]
    private RequestedByActor(ActorNumber actorNumber, ActorRole actorRole)
    {
        ActorNumber = actorNumber;
        ActorRole = actorRole;
    }

    public ActorNumber ActorNumber { get; init; }

    public ActorRole ActorRole { get; init; }

    public static RequestedByActor From(Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return new RequestedByActor(actor.ActorNumber, actor.ActorRole);
    }

    public static RequestedByActor From(ActorNumber actorNumber, ActorRole actorRole)
    {
        return new RequestedByActor(actorNumber, actorRole);
    }
}
