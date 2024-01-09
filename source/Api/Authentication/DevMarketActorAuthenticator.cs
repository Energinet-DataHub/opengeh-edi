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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.Authentication;

public class DevMarketActorAuthenticator : MarketActorAuthenticator
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<DevMarketActorAuthenticator> _logger;

    public DevMarketActorAuthenticator(
        IMasterDataClient masterDataClient,
        IDatabaseConnectionFactory connectionFactory,
        AuthenticatedActor authenticatedActor,
        ILogger<DevMarketActorAuthenticator> logger)
        : base(masterDataClient, authenticatedActor, logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public override async Task<bool> AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var azpTokenClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals(ClaimsMap.UserId, StringComparison.OrdinalIgnoreCase));
        _logger.LogInformation("claimsPrincipal: {ClaimsPrincipal}", string.Join(",", claimsPrincipal.Claims));
        if (!string.IsNullOrWhiteSpace(azpTokenClaim?.Value))
        {
            return await base.AuthenticateAsync(claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }

        var actorNumberClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals("test-actornumber", StringComparison.OrdinalIgnoreCase));
        if (actorNumberClaim is null)
        {
            _logger.LogInformation("No test-actornumber claim found. Falling back to default authentication.");
            return await base.AuthenticateAsync(claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }

        var actor = await FindActorAsync(actorNumberClaim.Value, cancellationToken).ConfigureAwait(false);
        if (actor is null)
        {
            _logger.LogInformation("No actor found for test-actornumber claim. Falling back to default authentication.");
            return await base.AuthenticateAsync(claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }

        var claimsWithUpdatedActor = ReplaceCurrent(claimsPrincipal, actor);

        return await base.AuthenticateAsync(claimsWithUpdatedActor, cancellationToken).ConfigureAwait(false);
    }

    private ClaimsPrincipal ReplaceCurrent(ClaimsPrincipal currentClaimsPrincipal, Actor actor)
    {
        var claims = currentClaimsPrincipal.Claims.Where(claim => !claim.Type.Equals(ClaimsMap.UserId, StringComparison.OrdinalIgnoreCase)).ToList();
        claims.Add(new Claim(ClaimsMap.UserId, actor.ExternalId));
        _logger.LogInformation($"Replaced current claims with {actor.ExternalId} for test-actornumber {actor.ActorNumber}.");
        var identity = new ClaimsIdentity(claims);
        return new ClaimsPrincipal(identity);
    }

    private async Task<Actor?> FindActorAsync(string actorNumber, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory
            .GetConnectionAndOpenAsync(cancellationToken)
            .ConfigureAwait(false);

        return await connection
            .QueryFirstOrDefaultAsync<Actor>(
                "SELECT ActorNumber, ExternalId FROM dbo.Actor WHERE ActorNumber = @ActorNumber",
                new { ActorNumber = actorNumber })
            .ConfigureAwait(false);
    }

    #pragma warning disable
    private record Actor(string ActorNumber, string ExternalId);
}
