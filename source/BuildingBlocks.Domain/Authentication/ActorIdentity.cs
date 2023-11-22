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
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.Common.Actors;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;

public class ActorIdentity
{
    public ActorIdentity(
        ActorNumber actorNumber,
        Restriction restriction,
        IEnumerable<MarketRole>? marketRoles = null)
    {
        ActorNumber = actorNumber;
        Restriction = restriction;
        MarketRoles = marketRoles;
    }

    public ActorNumber ActorNumber { get; }

    public Restriction Restriction { get; set; }

    private IEnumerable<MarketRole>? MarketRoles { get; set; }

    public bool HasRole(MarketRole role)
    {
        return MarketRoles != null && MarketRoles.Any(marketRole => marketRole.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase) &&
                                                                    marketRole.Code.Equals(role.Code, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasRestriction(Restriction suspect)
    {
        return suspect != null && Restriction.Name.Equals(suspect.Name, StringComparison.OrdinalIgnoreCase);
    }
}
