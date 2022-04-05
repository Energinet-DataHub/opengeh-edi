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
using System.Linq;
using System.Security.Claims;
using B2B.Transactions.Authentication;

namespace B2B.Transactions.Infrastructure.Authentication.MarketActors
{
    public class MarketActorAuthenticator : IMarketActorAuthenticator
    {
        public MarketActorIdentity CurrentIdentity { get; private set; } = new NotAuthenticated();

        public void Authenticate(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));
            var roles = claimsPrincipal.FindAll(claim =>
                    claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .ToArray();

            var id = GetClaimValueFrom(claimsPrincipal, "azp");
            var actorId = GetClaimValueFrom(claimsPrincipal, "actorid");
            var canParseIdentifierType = Enum.TryParse<MarketActorIdentity.IdentifierType>(GetClaimValueFrom(claimsPrincipal, "actoridtype"), true, out var identifierType);

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(actorId) || canParseIdentifierType == false)
            {
                CurrentIdentity = new NotAuthenticated();
            }
            else
            {
                CurrentIdentity = new Authenticated(id, actorId, identifierType, roles);
            }
        }

        private static string? GetClaimValueFrom(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?
                .Value;
        }
    }
}
