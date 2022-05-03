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
using System.Threading.Tasks;
using B2B.Transactions.Configuration.Authentication;
using B2B.Transactions.Infrastructure.Authentication;
using B2B.Transactions.Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Api.Configuration.Middleware.Authentication.MarketActors
{
    public class MarketActorAuthenticatorMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<MarketActorAuthenticatorMiddleware> _logger;

        public MarketActorAuthenticatorMiddleware(ILogger<MarketActorAuthenticatorMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));
            var marketActorAuthenticator = context.GetService<IMarketActorAuthenticator>();
            var currentClaimsPrincipal = context.GetService<CurrentClaimsPrincipal>();

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

            if (currentClaimsPrincipal.ClaimsPrincipal is null)
            {
                _logger.LogError("Current claims principal is null. Cannot continue.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            marketActorAuthenticator.Authenticate(currentClaimsPrincipal.ClaimsPrincipal!);
            if (marketActorAuthenticator.CurrentIdentity is NotAuthenticated)
            {
                _logger.LogError("Could not authenticate market actor identity. This is due to the current claims identity does hold the required claims.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var serializer = context.GetService<ISerializer>();
            WriteAuthenticatedIdentityToLog(marketActorAuthenticator, serializer);
            await next(context).ConfigureAwait(false);
        }

        private void WriteAuthenticatedIdentityToLog(IMarketActorAuthenticator marketActorAuthenticator, ISerializer serializer)
        {
            _logger.LogInformation("Successfully authenticated market actor identity.");
            _logger.LogInformation(serializer.Serialize(marketActorAuthenticator.CurrentIdentity));
        }
    }
}
