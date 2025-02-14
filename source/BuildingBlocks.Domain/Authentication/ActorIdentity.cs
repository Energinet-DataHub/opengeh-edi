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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;

public class ActorIdentity
{
    /// <summary>
    /// Create a new instance of <see cref="ActorIdentity"/>.
    /// </summary>
    /// <param name="actorNumber"></param>
    /// <param name="restriction"></param>
    /// <param name="actorRole"></param>
    /// <param name="actorClientId">The actor client id, typically retrieved from a B2B token.</param>
    /// <param name="actorId">The actor id, typically retrieved from a B2C token.</param>
    public ActorIdentity(
        ActorNumber actorNumber,
        Restriction restriction,
        ActorRole actorRole,
        Guid? actorClientId,
        Guid? actorId)
    {
        ActorNumber = actorNumber;
        Restriction = restriction;
        ActorRole = actorRole;
        ActorClientId = actorClientId;
        ActorId = actorId;
    }

    public ActorNumber ActorNumber { get; }

    public Restriction Restriction { get; }

    public ActorRole ActorRole { get; }

    /// <summary>
    /// Actor client id is only set if the current user has a B2B token.
    /// </summary>
    public Guid? ActorClientId { get; }

    /// <summary>
    /// Actor id is only set if the current user has a B2C token.
    /// </summary>
    public Guid? ActorId { get; }

    public bool HasRole(ActorRole role)
    {
        return ActorRole.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase) && ActorRole.Code.Equals(role.Code, StringComparison.OrdinalIgnoreCase);
    }

    public bool HasAnyOfRoles(params ActorRole[] roles)
    {
        return roles.Contains(ActorRole);
    }

    public bool HasRestriction(Restriction suspect)
    {
        return Restriction.Name.Equals(suspect.Name, StringComparison.OrdinalIgnoreCase);
    }

    public Actor ToActor()
    {
        return new Actor(ActorNumber, ActorRole);
    }
}
