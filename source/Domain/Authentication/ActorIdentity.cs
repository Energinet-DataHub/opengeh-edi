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

using Energinet.DataHub.EDI.Common.Actors;

namespace Energinet.DataHub.EDI.Domain.Authentication;

public class ActorIdentity
{
    public ActorIdentity(
        ActorNumber actorNumber,
        IEnumerable<MarketRole> roles,
        IEnumerable<Restriction> restrictions)
    {
        ActorNumber = actorNumber;
        Roles = roles;
        Restrictions = restrictions;
    }

    public ActorNumber ActorNumber { get; }

    public IEnumerable<Restriction> Restrictions { get; set; }

    private IEnumerable<MarketRole> Roles { get; set; }

    public bool HasRole(MarketRole role)
    {
        return Roles.Any(marketRole => marketRole.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase) &&
                                       marketRole.Code.Equals(role.Code, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasRestriction(Restriction suspect)
    {
        return Restrictions.Any(restriction => restriction.Name.Equals(suspect.Name, StringComparison.OrdinalIgnoreCase));
    }
}
