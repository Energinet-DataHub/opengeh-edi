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
using System.Threading.Tasks;
using Dapper;
using Messaging.Infrastructure.Configuration.Authentication;
using Messaging.Infrastructure.Configuration.DataAccess;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.Configuration.Middleware.Authentication.Bearer
{
    public class ClaimsEnrichmentMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<ClaimsEnrichmentMiddleware> _logger;

        public ClaimsEnrichmentMiddleware(ILogger<ClaimsEnrichmentMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));
            var currentClaimsPrincipal = context.GetService<CurrentClaimsPrincipal>();

            if (!context.IsRequestFromUser())
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

            if (currentClaimsPrincipal.ClaimsPrincipal is null)
            {
                _logger.LogError("No current authenticated user");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var marketActorId = GetMarketActorId(currentClaimsPrincipal.ClaimsPrincipal);
            if (string.IsNullOrEmpty(marketActorId))
            {
                _logger.LogError("Could not read market actor id from claims principal.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var connectionFactory = context.GetService<B2BContext>();
            var actor = await GetActorAsync(Guid.Parse(marketActorId), connectionFactory).ConfigureAwait(false);
            if (actor is null)
            {
                _logger.LogError($"Could not find an actor in the database with id {marketActorId}");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var identity = CreateClaimsIdentityFrom(actor, currentClaimsPrincipal);
            currentClaimsPrincipal.SetCurrentUser(new ClaimsPrincipal(identity));

            await next(context).ConfigureAwait(false);
        }

        private static string? GetMarketActorId(ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst(claim => claim.Type.Equals("azp", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static ClaimsIdentity CreateClaimsIdentityFrom(ActorForAuthentication actorForAuthentication, CurrentClaimsPrincipal currentClaimsPrincipal)
        {
            var claims = currentClaimsPrincipal.ClaimsPrincipal!.Claims.ToList();
            claims.Add(new Claim("actorid", actorForAuthentication.Identifier));
            claims.Add(new Claim("actoridtype", actorForAuthentication.IdentificationType));

            var currentIdentity = currentClaimsPrincipal.ClaimsPrincipal?.Identity as ClaimsIdentity;
            var identity = new ClaimsIdentity(
                claims,
                currentIdentity!.AuthenticationType,
                currentIdentity.NameClaimType,
                currentIdentity.RoleClaimType);
            return identity;
        }

        private static async Task<ActorForAuthentication?> GetActorAsync(Guid actorId, B2BContext context)
        {
            var sql = "SELECT TOP 1 [Id] AS ActorId,[IdentificationType],[IdentificationNumber] AS Identifier,[Roles] FROM [dbo].[Actor] WHERE Id = @ActorId";

            var result = await context.Database.GetDbConnection()
                .QuerySingleOrDefaultAsync<ActorForAuthentication>(sql, new { ActorId = actorId })
                .ConfigureAwait(false);

            return result;
        }
    }
}
