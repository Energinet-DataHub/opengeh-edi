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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.EDI.B2CWebApi.Exceptions;
using Energinet.DataHub.EDI.B2CWebApi.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Security;

public sealed class FrontendUserProvider : IUserProvider<FrontendUser>
{
    private readonly ILogger<FrontendUserProvider> _logger;
    private readonly AuthenticatedActor _authenticatedActor;

    public FrontendUserProvider(ILogger<FrontendUserProvider> logger, AuthenticatedActor authenticatedActor)
    {
        _logger = logger;
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
        string? marketRole = null;
        List<string> roles = [];
        foreach (var claim in claims)
        {
            if (actorNumber is not null
                && marketRole is not null)
            {
                break;
            }

            if (claim.Type.Equals("actornumber", StringComparison.OrdinalIgnoreCase))
                actorNumber = claim.Value;

            if (claim.Type.Equals("marketroles", StringComparison.OrdinalIgnoreCase))
                marketRole = claim.Value;

            if (claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
                roles.Add(claim.Value);
        }

        if (actorNumber is null)
            throw new MissingActorNumberException();

        if (marketRole is null)
            throw new MissingMarketRoleException();

        var frontendUser = new FrontendUser(
            userId,
            actorId,
            actorNumber,
            marketRole,
            roles.ToArray());

        _logger.LogInformation(
            "Provide front-end user, user id: {UserId}, actor id: {ActorId}, actor number: {ActorNumber}, market role: {MarketRole}, roles: {Roles}",
            frontendUser.UserId,
            frontendUser.ActorId,
            frontendUser.ActorNumber,
            frontendUser.MarketRole,
            string.Join(", ", frontendUser.Roles));

        SetAuthenticatedActor(ActorNumber.Create(actorNumber), accessAllData: multiTenancy, role: TryGetActorRole(marketRole));

        return Task.FromResult<FrontendUser?>(frontendUser);
    }

    private ActorRole? TryGetActorRole(string role)
    {
        try
        {
            var marketRole = EnumerationType.FromName<MarketRole>(role);

            // DataHubAdministrator does not have a corresponding actor role
            if (marketRole == MarketRole.DataHubAdministrator)
                return null;

            if (marketRole.Code == null)
                throw new InvalidOperationException("Market role code is null");

            var actorRole = ActorRole.FromCode(marketRole.Code);
            return actorRole;
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                e,
                "Failed to parse front-end user's market role to actor role (market role: {Role})",
                role);

            return null;
        }
    }

    private void SetAuthenticatedActor(ActorNumber actorNumber, bool accessAllData, ActorRole? role)
    {
        var restriction = accessAllData ? Restriction.None : Restriction.Owned;

        _logger.LogInformation(
            "Set authenticated actor, actor number: {ActorNumber}, role: {ActorRole}, restriction: {Restriction})",
            actorNumber.Value,
            role,
            restriction);

        _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(
            actorNumber: actorNumber,
            restriction: restriction,
            marketRole: role));
    }
}
