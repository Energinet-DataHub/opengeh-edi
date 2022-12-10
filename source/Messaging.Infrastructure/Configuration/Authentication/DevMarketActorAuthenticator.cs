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
using System.Security.Claims;
using System.Threading.Tasks;
using Messaging.Application.Actors;
using Messaging.Infrastructure.Actors;

namespace Messaging.Infrastructure.Configuration.Authentication;

public class DevMarketActorAuthenticator : MarketActorAuthenticator
{
    private readonly IActorRegistry _actorRegistry;

    public DevMarketActorAuthenticator(IActorLookup actorLookup, IActorRegistry actorRegistry)
        : base(actorLookup)
    {
        _actorRegistry = actorRegistry;
    }

    public override async Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var actorNumberClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals("test-actornumber", StringComparison.OrdinalIgnoreCase));
        var userIdClaim = claimsPrincipal.FindFirst(claim => claim.Type.Equals(ClaimsMap.UserId, StringComparison.OrdinalIgnoreCase));
        if (actorNumberClaim is not null && userIdClaim is not null)
        {
            await _actorRegistry.TryStoreAsync(
                new CreateActor(
                    Guid.NewGuid().ToString(),
                    userIdClaim.Value,
                    actorNumberClaim.Value)).ConfigureAwait(false);
        }

        await base.AuthenticateAsync(claimsPrincipal).ConfigureAwait(false);
    }
}
