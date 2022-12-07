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
using Messaging.Domain.Actors;

namespace Messaging.Infrastructure.Configuration.Authentication;

public static class ClaimsMap
{
    private static readonly Dictionary<string, MarketRole> _rolesMap = new()
    {
        { "electricalsupplier", MarketRole.EnergySupplier },
        { "gridoperator", MarketRole.GridOperator },
    };

    public static string UserId => "azp";

    public static MarketRole? RoleFrom(string roleClaimValue)
    {
        _rolesMap.TryGetValue(roleClaimValue, out var marketRole);
        return marketRole;
    }

    public static Claim RoleFrom(MarketRole marketRole)
    {
        return new Claim(ClaimTypes.Role, _rolesMap.First(x => x.Value.Equals(marketRole)).Key);
    }
}
