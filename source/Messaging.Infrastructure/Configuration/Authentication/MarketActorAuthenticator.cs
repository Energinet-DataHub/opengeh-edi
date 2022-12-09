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

        public MarketActorAuthenticator(IActorLookup actorLookup)
        {
            _actorLookup = actorLookup;
        }

        public MarketActorIdentity CurrentIdentity { get; private set; } = new NotAuthenticated();

        public async Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));

            var userIdFromSts = GetClaimValueFrom(claimsPrincipal, ClaimsMap.UserId);
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

            var roles = ParseRoles(claimsPrincipal);
            if (roles.Count == 0)
            {
                ActorIsNotAuthorized();
                return;
            }

            CurrentIdentity = new Authenticated(userIdFromSts, actorNumber, roles);
        }

        private static string? GetClaimValueFrom(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?
                .Value;
        }

        private static IReadOnlyList<MarketRole> ParseRoles(ClaimsPrincipal claimsPrincipal)
        {
            var roleClaims = claimsPrincipal.FindAll(claim =>
                    claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value);

            var roles = new List<MarketRole>();
            foreach (var roleClaim in roleClaims)
            {
                var marketRole = ClaimsMap.RoleFrom(roleClaim);
                if (marketRole is not null)
                {
                    roles.Add(marketRole);
                }
            }

            return roles;
        }

        private void ActorIsNotAuthorized()
        {
            CurrentIdentity = new NotAuthenticated();
        }
    }
}
