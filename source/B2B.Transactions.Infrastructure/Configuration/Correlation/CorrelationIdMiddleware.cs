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
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraceContext = B2B.Transactions.Infrastructure.Configuration.Correlation;

namespace B2B.Transactions.Infrastructure.Configuration.Correlation
{
    public class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            ILogger<CorrelationIdMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, [NotNull] FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Parsed TraceContext: " + context.TraceContext.TraceParent ?? string.Empty);
            var traceContext = TraceContext.Parse(context.TraceContext.TraceParent);

            var correlationContext = context.InstanceServices.GetRequiredService<CorrelationContext>();
            correlationContext.SetId(traceContext.TraceId);
            correlationContext.SetParentId(traceContext.ParentId);

            await next(context).ConfigureAwait(false);
        }
    }
}
