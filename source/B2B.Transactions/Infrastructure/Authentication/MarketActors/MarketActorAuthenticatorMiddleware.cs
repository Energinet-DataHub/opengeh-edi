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
using System.Net;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Authentication.Bearer;
using Energinet.DataHub.Core.App.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Authentication.MarketActors
{
    public class MarketActorAuthenticatorMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly CurrentClaimsPrincipal _currentClaimsPrincipal;
        private readonly MarketActorAuthenticator _marketActorAuthenticator;
        private readonly ILogger<MarketActorAuthenticatorMiddleware> _logger;

        public MarketActorAuthenticatorMiddleware(CurrentClaimsPrincipal currentClaimsPrincipal, MarketActorAuthenticator marketActorAuthenticator, ILogger<MarketActorAuthenticatorMiddleware> logger)
        {
            _currentClaimsPrincipal = currentClaimsPrincipal ?? throw new ArgumentNullException(nameof(currentClaimsPrincipal));
            _marketActorAuthenticator = marketActorAuthenticator ?? throw new ArgumentNullException(nameof(marketActorAuthenticator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogError("Current claims principal is null. Cannot continue.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            _marketActorAuthenticator.Authenticate(_currentClaimsPrincipal.ClaimsPrincipal!);
            if (_marketActorAuthenticator.CurrentIdentity is NotAuthenticated)
            {
                _logger.LogError("Could not authenticate market actor identity. This is due to the current claims identity does hold the required claims.");
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            _logger.LogInformation("Successfully authenticated market actor identity.");
            await next(context).ConfigureAwait(false);
        }
    }
}
