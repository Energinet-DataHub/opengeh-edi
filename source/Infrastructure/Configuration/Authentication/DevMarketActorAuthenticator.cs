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
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Domain.Actors;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;

public class DevMarketActorAuthenticator : MarketActorAuthenticator
{
    private readonly IActorRegistry _actorRegistry;
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public DevMarketActorAuthenticator(IActorRepository actorRepository, IActorRegistry actorRegistry, IDatabaseConnectionFactory connectionFactory)
        : base(actorRepository)
    {
        _actorRegistry = actorRegistry;
        _connectionFactory = connectionFactory;
    }

    public override async Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var actorNumberClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals("test-actornumber", StringComparison.OrdinalIgnoreCase));
        if (actorNumberClaim is not null)
        {
            var actor = await FindActorAsync(actorNumberClaim.Value, cancellationToken).ConfigureAwait(false);
            if (actor is null)
            {
                actor = new Actor(Guid.NewGuid(), actorNumberClaim.Value);
                await RegisterActorAsync(actor, cancellationToken).ConfigureAwait(false);
            }

            await base.AuthenticateAsync(ReplaceCurrent(claimsPrincipal, actor), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await base.AuthenticateAsync(claimsPrincipal, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ClaimsPrincipal ReplaceCurrent(ClaimsPrincipal currentClaimsPrincipal, Actor actor)
    {
        var claims = currentClaimsPrincipal.Claims.Where(claim => !claim.Type.Equals(ClaimsMap.UserId, StringComparison.OrdinalIgnoreCase)).ToList();
        claims.Add(new Claim(ClaimsMap.UserId, actor.Id.ToString()));

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
                "SELECT ExternalId AS Id, ActorNumber AS Number FROM dbo.Actor WHERE ActorNumber = @ActorNumber",
                new { ActorNumber = actorNumber })
            .ConfigureAwait(false);
    }

    private Task RegisterActorAsync(Actor actor, CancellationToken cancellationToken)
    {
        return _actorRegistry.TryStoreAsync(new CreateActorCommand(actor.Id.ToString(), ActorNumber.Create(actor.Number)), cancellationToken);
    }

    #pragma warning disable
    private record Actor(Guid Id, string Number);
}
