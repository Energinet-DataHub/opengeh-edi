﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.EDI.B2CWebApi.Exceptions;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Security;

public sealed class FrontendUserProvider : IUserProvider<FrontendUser>
{
    private readonly AuthenticatedActor _authenticatedActor;

    public FrontendUserProvider(AuthenticatedActor authenticatedActor)
    {
        _authenticatedActor = authenticatedActor;
    }

    public Task<FrontendUser?> ProvideUserAsync(
        Guid userId,
        Guid actorId,
        bool multiTenancy,
        IEnumerable<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        string? actorNumber = null;
        string? role = null;
        string? azp = null;
        foreach (var claim in claims)
        {
            if (actorNumber is not null
                && role is not null
                && azp is not null)
            {
                break;
            }

            if (claim.Type.Equals("actornumber", StringComparison.OrdinalIgnoreCase))
                actorNumber = claim.Value;

            if (claim.Type.Equals("marketroles", StringComparison.OrdinalIgnoreCase))
                role = claim.Value;

            if (claim.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))
                azp = claim.Value;
        }

        if (actorNumber is null)
            throw new MissingActorNumberException();

        if (role is null)
            throw new MissingRoleException();

        if (azp is null)
            throw new MissingAzpException();

        SetAuthenticatedActor(ActorNumber.Create(actorNumber), accessAllData: multiTenancy, role: TryGetMarketRole(role));
        return Task.FromResult<FrontendUser?>(new FrontendUser(
            userId,
            actorId,
            multiTenancy,
            actorNumber,
            role,
            azp));
    }

    private static ActorRole? TryGetMarketRole(string role)
    {
        try
        {
            return EnumerationType.FromName<ActorRole>(role);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private void SetAuthenticatedActor(ActorNumber actorNumber, bool accessAllData, ActorRole? role)
    {
        if (accessAllData)
        {
            _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
                actorNumber,
                restriction: Restriction.None,
                marketRole: role));
        }
        else
        {
            _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
                actorNumber,
                restriction: Restriction.Owned,
                marketRole: role));
        }
    }
}
