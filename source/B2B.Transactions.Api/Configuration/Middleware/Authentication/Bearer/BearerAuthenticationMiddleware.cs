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
using System.Text;
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Configuration.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Api.Configuration.Middleware.Authentication.Bearer
{
    public class BearerAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<BearerAuthenticationMiddleware> _logger;

        public BearerAuthenticationMiddleware(ILogger<BearerAuthenticationMiddleware> logger)
        {
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

            var jwtTokenParser = context.GetService<JwtTokenParser>();
            var result = jwtTokenParser.ParseFrom(httpRequestData.Headers);
            if (result.Success == false)
            {
                LogParseResult(result);
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            var currentClaimsPrincipal = context.GetService<CurrentClaimsPrincipal>();
            currentClaimsPrincipal.SetCurrentUser(result.ClaimsPrincipal!);
            _logger.LogInformation("Bearer token authentication succeeded.");
            await next(context).ConfigureAwait(false);
        }

        private void LogParseResult(Result result)
        {
            var message = new StringBuilder();
            message.AppendLine("Failed to parse claims principal from JWT:");
            message.AppendLine(result.Error?.Message);
            message.AppendLine("Token from HTTP request header:");
            message.AppendLine(result.Token);
            _logger.LogError(message.ToString());
        }
    }
}
