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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using B2B.Transactions.Api.Servicebus;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Infrastructure.Configuration.Servicebus;

public class ServiceBusMessageMetadataMiddleWare : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger;

    public ServiceBusMessageMetadataMiddleWare(ILogger logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, [NotNull] FunctionExecutionDelegate next)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var correlationContext = context.GetService<ICorrelationContext>();
        var serializer = context.GetService<ISerializer>();

        context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

        if (serviceBusMessageMetadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        var metadata = serializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
        correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property Correlation-ID is missing"));

        _logger.LogInformation("Dequeued service bus message with correlation id: " + correlationContext.Id ?? string.Empty);

        await next(context).ConfigureAwait(false);
    }
}
