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
using Dapper;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Domain.Actors;

namespace Messaging.Infrastructure.Configuration.Authentication
{
    public class MarketActorAuthenticator : IMarketActorAuthenticator
    {
        private readonly IDbConnectionFactory _connectionFactory;

        private readonly Dictionary<string, MarketRole> _rolesMap = new()
        {
            { "electricalsupplier", MarketRole.EnergySupplier },
            { "gridoperator", MarketRole.GridOperator },
        };

        public MarketActorAuthenticator(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public MarketActorIdentity CurrentIdentity { get; private set; } = new NotAuthenticated();

        public async Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal == null) throw new ArgumentNullException(nameof(claimsPrincipal));
            var roles = claimsPrincipal.FindAll(claim =>
                    claim.Type.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .ToArray();

            var id = GetClaimValueFrom(claimsPrincipal, "azp");
            if (string.IsNullOrWhiteSpace(id))
            {
                CurrentIdentity = new NotAuthenticated();
                return;
            }

            var actorId = await GetActorNumberAsync(Guid.Parse(id)).ConfigureAwait(false);
            if (actorId is null)
            {
                CurrentIdentity = new NotAuthenticated();
                return;
            }

            var marketRole = ParseMarketRoleFrom(roles);
            if (marketRole is null)
            {
                CurrentIdentity = new NotAuthenticated();
                return;
            }

            CurrentIdentity = new Authenticated(id, actorId, roles, marketRole);
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

        private async Task<ActorNumber?> GetActorNumberAsync(Guid actorId)
        {
            var sql = "SELECT TOP 1 [Id] AS ActorId, [IdentificationNumber] AS Identifier FROM [b2b].[Actor] WHERE B2CId=@ActorId";

            var result = await _connectionFactory.GetOpenConnection()
                .QuerySingleOrDefaultAsync<ActorForAuthentication>(sql, new { ActorId = actorId })
                .ConfigureAwait(false);

            if (result is null)
            {
                return null;
            }

            return ActorNumber.Create(result.Identifier);
        }

        private record ActorForAuthentication(Guid ActorId, string Identifier);
    }
}
