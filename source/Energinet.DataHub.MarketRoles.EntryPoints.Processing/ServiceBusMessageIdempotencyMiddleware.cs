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
using Energinet.DataHub.MarketRoles.Infrastructure.Messaging.Idempotency;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Processing
{
    public class ServiceBusMessageIdempotencyMiddleware : IFunctionsWorkerMiddleware
    {
        private const string MessageIdKey = "MessageId";
        private const string MessageTypeKey = "Label";
        private readonly ILogger _logger;
        private readonly IIncomingMessageRegistry _incomingMessageRegistry;

        public ServiceBusMessageIdempotencyMiddleware(ILogger logger, IIncomingMessageRegistry incomingMessageRegistry)
        {
            _logger = logger;
            _incomingMessageRegistry = incomingMessageRegistry ?? throw new ArgumentNullException(nameof(incomingMessageRegistry));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (next == null) throw new ArgumentNullException(nameof(next));

            var messageType = GetValueFromMessage(context, MessageTypeKey);
            var messageId = GetValueFromMessage(context, MessageIdKey);
            if (messageId != null && messageType != null)
            {
                await RegisterIdempotentMessageAsync(messageId, messageType).ConfigureAwait(false);
            }
            else
            {
                //TODO: Should we stop the flow or accept ??
                _logger?.LogCritical($"{MessageIdKey} and {MessageIdKey} must be present on incoming Service Bus message to enforce message idempotency.");
            }

            await next(context).ConfigureAwait(false);
        }

        private static string? GetValueFromMessage(FunctionContext context, string key)
        {
            context.BindingContext.BindingData.TryGetValue(key, out var value);
            return value?.ToString();
        }

        private async Task RegisterIdempotentMessageAsync(string messageId, string messageType)
        {
            await _incomingMessageRegistry.RegisterMessageAsync(messageId, messageType).ConfigureAwait(false);
            _logger?.LogTrace($"Registered incoming message id {messageId} of type {messageType}");
        }
    }
}
