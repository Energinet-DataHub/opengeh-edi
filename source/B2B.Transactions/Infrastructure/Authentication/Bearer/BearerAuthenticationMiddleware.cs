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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Authentication.Bearer
{
    public class BearerAuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly JwtTokenParser _jwtTokenParser;
        private readonly ILogger<BearerAuthenticationMiddleware> _logger;
        private readonly CurrentClaimsPrincipal _currentClaimsPrincipal;

        public BearerAuthenticationMiddleware(JwtTokenParser jwtTokenParser, ILogger<BearerAuthenticationMiddleware> logger, CurrentClaimsPrincipal currentClaimsPrincipal)
        {
            _jwtTokenParser = jwtTokenParser ?? throw new ArgumentNullException(nameof(jwtTokenParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentClaimsPrincipal = currentClaimsPrincipal ?? throw new ArgumentNullException(nameof(currentClaimsPrincipal));
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

            var result = _jwtTokenParser.ParseFrom(httpRequestData.Headers);
            if (result.Success == false)
            {
                LogParseResult(result);
                context.RespondWithUnauthorized(httpRequestData);
                return;
            }

            _currentClaimsPrincipal.SetCurrentUser(result.ClaimsPrincipal!);
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
