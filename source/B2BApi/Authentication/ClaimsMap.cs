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

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2BApi.Authentication;

public static class ClaimsMap
{
    private static readonly Dictionary<string, ActorRole> _rolesMap = new()
    {
        { "energysupplier", ActorRole.EnergySupplier },
        { "gridaccessprovider", ActorRole.GridOperator },
        { "metereddataresponsible", ActorRole.MeteredDataResponsible },
        { "balanceresponsibleparty", ActorRole.BalanceResponsibleParty },
        { "systemoperator", ActorRole.SystemOperator },
        { "gridoperator", ActorRole.GridOperator },
        { "delegated", ActorRole.Delegated },
    };

    public static string UserId => "azp";

    public static string Roles => "roles";

    public static ActorRole? RoleFrom(string roleClaimValue)
    {
        _rolesMap.TryGetValue(roleClaimValue, out var marketRole);
        return marketRole;
    }

    public static Claim RoleFrom(ActorRole actorRole)
    {
        return new Claim(Roles, _rolesMap.First(x => x.Value.Equals(actorRole)).Key);
    }
}
