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
using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.Configuration.Middleware.Correlation
{
    public class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<CorrelationIdMiddleware> _logger;
        private readonly ISerializer _serializer;

        public CorrelationIdMiddleware(
            ILogger<CorrelationIdMiddleware> logger,
            ISerializer serializer)
        {
            _logger = logger;
            _serializer = serializer;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            if (context.Is(FunctionContextExtensions.TriggerType.HttpTrigger))
            {
                var correlationId = ParseCorrelationIdFromHeader(context);
                if (string.IsNullOrEmpty(correlationId))
                {
                    throw new InvalidOperationException($"Could not parse correlation id from HTTP header.");
                }

                _logger.LogInformation($"Correlation id is: {correlationId}");
                var correlationContext = context.GetService<ICorrelationContext>();
                correlationContext.SetId(correlationId);
            }

            await next(context).ConfigureAwait(false);
        }

        private string? ParseCorrelationIdFromHeader(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("Headers", out var headersObj);

            if (headersObj is not string headersStr)
            {
                return null;
            }

            var headers = _serializer.Deserialize<Dictionary<string, string>>(headersStr);

            #pragma warning disable CA1308 // Use lower case
            var normalizedKeyHeaders = headers
                .ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value);
            #pragma warning restore

            normalizedKeyHeaders.TryGetValue("correlationid", out var correlationId);

            return correlationId;
        }
    }
}
