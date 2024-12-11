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

using System.Security.Claims;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Authentication;

public class MarketActorAuthenticator : IMarketActorAuthenticator
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ILogger<MarketActorAuthenticator> _logger;

    public MarketActorAuthenticator(
        IMasterDataClient masterDataClient,
        AuthenticatedActor authenticatedActor,
        ILogger<MarketActorAuthenticator> logger)
    {
        _masterDataClient = masterDataClient;
        _authenticatedActor = authenticatedActor;
        _logger = logger;
    }

    public virtual async Task<bool> AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);
        _logger.LogDebug("claimsPrincipal: {ClaimsPrincipal}", string.Join(",", claimsPrincipal.Claims));
        var rolesFromClaims = GetClaimValuesFrom(claimsPrincipal, ClaimsMap.Roles);
        var role = ParseRole(rolesFromClaims);

        var actorIdFromAzp = GetClaimValueFrom(claimsPrincipal, ClaimsMap.ActorId);
        _logger.LogDebug("azp claim value: {Azp}", actorIdFromAzp ?? "null");
        var actorNumber = !string.IsNullOrEmpty(actorIdFromAzp)
            ? await _masterDataClient.GetActorNumberByExternalIdAsync(actorIdFromAzp, cancellationToken)
                .ConfigureAwait(false)
            : null;

        return Authenticate(actorNumber, role, actorIdFromAzp);
    }

    public bool Authenticate(ActorNumber? actorNumber, ActorRole? actorRole, string? actorId)
    {
        if (actorNumber is null)
        {
            _logger.LogError("Could not authenticate market actor identity. This is due to missing actorNumber for the Azp token");
            return false;
        }

        if (actorRole is null)
        {
            _logger.LogError(
                @"Could not authenticate market actor identity.
                    This is due to missing marketRole in the http request data claims for ActorNumber: {ActorNumber}.",
                actorNumber.Value);
            return false;
        }

        if (actorId is null)
        {
            // This is only possible in the case of certificate authentication, since the actor number above
            // is retrieved from the actor id in case of token authentication.
            _logger.LogWarning("Authenticated market actor identity has no actor id (ActorNumber={ActorNumber}, ActorRole={ActorRole}).", actorId, actorNumber.Value, actorRole.Code);
        }

        var actorIdGuid = Guid.TryParse(actorId, out var guidParseResult) ? guidParseResult : (Guid?)null;
        if (actorIdGuid is null)
            _logger.LogWarning("Authenticated market actor identity has no valid actor id (ActorId={ActorId}, ActorNumber={ActorNumber}, ActorRole={ActorRole}).", actorId, actorNumber.Value, actorRole.Code);

        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
            actorNumber,
            Restriction.Owned,
            actorRole: actorRole,
            actorIdGuid));
        return true;
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

    private static ActorRole? ParseRole(IEnumerable<string> roles)
    {
        var roleList = roles.ToList();
        if (roleList.Count == 0 || roleList.Count > 1)
        {
            return null;
        }

        return ClaimsMap.RoleFrom(roleList.Single());
    }
}
