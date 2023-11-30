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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;

namespace Energinet.DataHub.EDI.Api.Authentication
{
    public class MarketActorAuthenticator : IMarketActorAuthenticator
    {
        private readonly IActorRepository _actorRepository;
        private readonly AuthenticatedActor _authenticatedActor;

        public MarketActorAuthenticator(IActorRepository actorRepository, AuthenticatedActor authenticatedActor)
        {
            _actorRepository = actorRepository;
            _authenticatedActor = authenticatedActor;
        }

        public virtual async Task<bool> AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
        {
            if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));

            var token = GetClaimValueFrom(claimsPrincipal, "token");

            // TODO: Temporarly hack for authorizing users originating from portal. Remove when JWT is updated
            if (UserOriginatesFromPortal(token))
            {
                _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create("1234512345888"), Restriction.Owned));
                return true;
            }

            var rolesFromClaims = GetClaimValuesFrom(claimsPrincipal, ClaimTypes.Role);
            var role = ParseRole(rolesFromClaims);

            var userIdFromAzp = GetClaimValueFrom(claimsPrincipal, ClaimsMap.UserId);
            var actorNumber = !string.IsNullOrEmpty(userIdFromAzp)
                ? await _actorRepository.GetActorNumberByExternalIdAsync(userIdFromAzp, cancellationToken).ConfigureAwait(false)
                : null;

            return Authenticate(actorNumber, role);
        }

        public bool Authenticate(ActorNumber? actorNumber, MarketRole? marketRole)
        {
            if (marketRole is null)
                return false;

            if (actorNumber is null)
                return false;

            _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(actorNumber, Restriction.Owned, marketRole: marketRole));
            return true;
        }

        private static bool UserOriginatesFromPortal(string? token)
        {
            return token != null;
        }

        private static string? GetClaimValueFrom(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))?
                .Value;
        }

        private static IEnumerable<string> GetClaimValuesFrom(ClaimsPrincipal claimsPrincipal, string claimName)
        {
            return claimsPrincipal.FindAll(claim => claim.Type.Equals(claimName, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value);
        }

        private static MarketRole? ParseRole(IEnumerable<string> roles)
        {
            var roleList = roles.ToList();
            if (!roleList.Any() || roleList.Count > 1)
            {
                return null;
            }

            return ClaimsMap.RoleFrom(roleList.Single());
        }
    }
}
