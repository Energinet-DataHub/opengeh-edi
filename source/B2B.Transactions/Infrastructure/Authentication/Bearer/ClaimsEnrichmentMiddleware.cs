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
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Authentication.Bearer
{
    public class ClaimsEnrichmentMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly CurrentClaimsPrincipal _currentClaimsPrincipal;
        private readonly ILogger<ClaimsEnrichmentMiddleware> _logger;
        private readonly IDbConnectionFactory _connectionFactory;

        public ClaimsEnrichmentMiddleware(CurrentClaimsPrincipal currentClaimsPrincipal, ILogger<ClaimsEnrichmentMiddleware> logger, IDbConnectionFactory connectionFactory)
        {
            _currentClaimsPrincipal = currentClaimsPrincipal ?? throw new ArgumentNullException(nameof(currentClaimsPrincipal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionFactory = connectionFactory;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (!context.Is(FunctionContextExtensions.TriggerType.HttpTrigger))
            {
                _logger.LogInformation("Functions is not triggered by HTTP. Call next middleware.");
                await next(context).ConfigureAwait(false);
                return;
            }

            var httpRequestData = context.GetHttpRequestData();
            if (httpRequestData == null)
            {
                _logger.LogTrace("No HTTP request data was available.");
                await next(context).ConfigureAwait(false);
                return;
            }

            if (_currentClaimsPrincipal.ClaimsPrincipal is null)
            {
                _logger.LogError("No current authenticated user");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var marketActorId = GetMarketActorId(_currentClaimsPrincipal.ClaimsPrincipal);
            if (string.IsNullOrEmpty(marketActorId))
            {
                _logger.LogError("Could not read market actor id from claims principal.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var actor = await GetActorAsync(Guid.Parse(marketActorId)).ConfigureAwait(false);
            if (actor is null)
            {
                _logger.LogError($"Could not find an actor in the database with id {marketActorId}");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var identity = CreateClaimsIdentityFrom(actor);
            _currentClaimsPrincipal.SetCurrentUser(new ClaimsPrincipal(identity));

            await next(context).ConfigureAwait(false);
        }

        private static string? GetMarketActorId(ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private ClaimsIdentity CreateClaimsIdentityFrom(Actor actor)
        {
            var claims = _currentClaimsPrincipal.ClaimsPrincipal!.Claims.ToList();
            claims.Add(new Claim("actorid", actor.Identifier));
            claims.Add(new Claim("actoridtype", actor.IdentificationType));

            var currentIdentity = _currentClaimsPrincipal.ClaimsPrincipal?.Identity as ClaimsIdentity;
            var identity = new ClaimsIdentity(
                claims,
                currentIdentity!.AuthenticationType,
                currentIdentity.NameClaimType,
                currentIdentity.RoleClaimType);
            return identity;
        }

        private async Task<Actor?> GetActorAsync(Guid actorId)
        {
            var sql = "SELECT TOP 1 [Id] AS ActorId,[IdentificationType],[IdentificationNumber] AS Identifier,[Roles] FROM [dbo].[Actor] WHERE Id = @ActorId";

            var result = await _connectionFactory
                .GetOpenConnection()
                .QuerySingleOrDefaultAsync<Actor>(sql, new { ActorId = actorId })
                .ConfigureAwait(false);

            return result;
        }
    }
}
