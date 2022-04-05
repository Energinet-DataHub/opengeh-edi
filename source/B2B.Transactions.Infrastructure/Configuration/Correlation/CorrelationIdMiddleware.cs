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
using Microsoft.Extensions.Logging;
using TraceContext = B2B.Transactions.Infrastructure.Configuration.Correlation;

namespace B2B.Transactions.Infrastructure.Configuration.Correlation
{
    public class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(
            ICorrelationContext correlationContext, ILogger<CorrelationIdMiddleware> logger)
        {
            _correlationContext = correlationContext;
            _logger = logger;
        }

        public async Task Invoke(FunctionContext context, [NotNull] FunctionExecutionDelegate next)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _logger.LogInformation("Parsed TraceContext: " + context.TraceContext.TraceParent ?? string.Empty);
            var traceContext = TraceContext.Parse(context.TraceContext.TraceParent);

            _logger.LogInformation("Setting TraceId: " + traceContext.TraceId ?? "null");
            _correlationContext.SetId(traceContext.TraceId);
            _logger.LogInformation("Actual correlation Id: " + _correlationContext.Id ?? "null");
            _logger.LogInformation("Setting ParentId: " + traceContext.ParentId ?? "null");
            _correlationContext.SetParentId(traceContext.ParentId);

            await next(context).ConfigureAwait(false);
        }
    }
}
