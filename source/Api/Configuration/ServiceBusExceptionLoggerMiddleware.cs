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
using Energinet.DataHub.EDI.Api.Configuration.Middleware;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.Configuration;

public class ServiceBusExceptionLoggerMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ServiceBusExceptionLoggerMiddleware> _logger;

    public ServiceBusExceptionLoggerMiddleware(ILogger<ServiceBusExceptionLoggerMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        _logger.LogWarning("ServiceBusExceptionLoggerMiddleware invoked");

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Is(FunctionContextExtensions.TriggerType.ServiceBusTrigger))
        {
            _logger.LogWarning("ServiceBusExceptionLoggerMiddleware is processing a ServiceBus message");

            try
            {
                await next(context).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                var logStatement = "Service bus message processing failed with: ";
                logStatement += context.RetryContext is null
                    ? "No retry context available."
                    : $"Retry count {context.RetryContext.RetryCount} of {context.RetryContext.MaxRetryCount}.";

                _logger.LogWarning(logStatement);

                // The RetryContext is potentially null!
                if (context.RetryContext?.RetryCount != context.RetryContext?.MaxRetryCount)
                {
                    _logger.LogWarning(
                        "Service bus message processing failed with exception {Exception}",
                        e);
                }
                else
                {
                    throw;
                }
            }
        }
        else
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
