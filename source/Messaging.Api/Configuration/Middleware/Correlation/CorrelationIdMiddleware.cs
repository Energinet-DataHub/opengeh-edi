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
using Energinet.DataHub.MeteringPoints.EntryPoints.Common;
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

            var correlationContext = context.GetService<ICorrelationContext>();

            var correlationId = default(string);
            if (context.Is(FunctionContextExtensions.TriggerType.HttpTrigger))
            {
                correlationId = ParseCorrelationIdFromHeader(context);
            }
            else if (context.Is(FunctionContextExtensions.TriggerType.ServiceBusTrigger))
            {
                correlationId = ParseCorrelationIdFromMessage(context);
            }

            if (correlationId is null)
            {
                correlationId = Guid.NewGuid().ToString();
                LogIfSet(correlationId, "Create new correlation id.");
            }

            correlationContext.SetId(correlationId);

            await next(context).ConfigureAwait(false);
        }

        private string? ParseFromMessageProperty(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("CorrelationId", out var correlationIdValue);
            if (correlationIdValue is string correlationId)
            {
                LogIfSet(correlationId, "Correlation is parsed from CorrelationId property on ServiceBus message.");
                return correlationId;
            }

            return null;
        }

        private string? ParseFromUserProperties(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("UserProperties", out var userPropertiesValue);
            if (userPropertiesValue is string userProperties)
            {
                var parsedUserProperties = _serializer.Deserialize<UserProperties>(userProperties);
                {
                    LogIfSet(parsedUserProperties.OperationCorrelationId, "Correlation is parsed from UserProperties on ServiceBus message.");
                    return parsedUserProperties.OperationCorrelationId;
                }
            }

            return null;
        }

        private string? ParseCorrelationIdFromMessage(FunctionContext context)
        {
            var correlationId = ParseFromUserProperties(context);
            return correlationId ?? ParseFromMessageProperty(context);
        }

        private string? ParseCorrelationIdFromHeader(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("Headers", out var headersObj);

            if (headersObj is not string headersStr)
            {
                throw new InvalidOperationException("Could not read headers");
            }

            var headers = _serializer.Deserialize<Dictionary<string, string>>(headersStr);

            #pragma warning disable CA1308 // Use lower case
            var normalizedKeyHeaders = headers
                .ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value);
            #pragma warning restore

            normalizedKeyHeaders.TryGetValue("correlationid", out var correlationId);
            LogIfSet(correlationId, "Correlation is parsed from HTTP header.");
            return correlationId;
        }

        private void LogIfSet(string? correlationId, string text)
        {
            if (correlationId is not null)
            {
                _logger.LogInformation(text);
                _logger.LogInformation($"Correlation id is: {correlationId}");
            }
        }
    }
}
