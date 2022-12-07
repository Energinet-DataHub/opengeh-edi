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
using System.Security.Claims;
using System.Threading.Tasks;
using Messaging.Application.Actors;
using Messaging.Application.Configuration.Authentication;
using Messaging.Domain.Actors;

namespace Messaging.Infrastructure.Configuration.Authentication
{
    public class MarketActorAuthenticator : IMarketActorAuthenticator
    {
        private readonly IActorLookup _actorLookup;

        private readonly Dictionary<string, MarketRole> _rolesMap = new()
        {
            { "electricalsupplier", MarketRole.EnergySupplier },
            { "gridoperator", MarketRole.GridOperator },
        };

        public MarketActorAuthenticator(IActorLookup actorLookup)
        {
            _actorLookup = actorLookup;
        }

        public MarketActorIdentity CurrentIdentity { get; private set; } = new NotAuthenticated();

        public async Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));
            var roles = claimsPrincipal.FindAll(claim =>
                    claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .ToArray();

            var userIdFromSts = GetClaimValueFrom(claimsPrincipal, "azp");
            if (string.IsNullOrWhiteSpace(userIdFromSts))
            {
                ActorIsNotAuthorized();
                return;
            }

            var actorNumber = await _actorLookup.GetActorNumberByB2CIdAsync(Guid.Parse(userIdFromSts)).ConfigureAwait(false);
            if (actorNumber is null)
            {
                ActorIsNotAuthorized();
                return;
            }

            var marketRole = ParseMarketRoleFrom(roles);
            if (marketRole is null)
            {
                ActorIsNotAuthorized();
                return;
            }

            CurrentIdentity = new Authenticated(userIdFromSts, actorNumber, roles, marketRole);
        }

        private static string? GetClaimValueFrom(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?
                .Value;
        }

        private MarketRole? ParseMarketRoleFrom(IEnumerable<string> roles)
        {
            var role = roles.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            return _rolesMap.TryGetValue(role, out var marketRole) == false
                ? null
                : marketRole;
        }

        private void ActorIsNotAuthorized()
        {
            CurrentIdentity = new NotAuthenticated();
        }
    }
}
