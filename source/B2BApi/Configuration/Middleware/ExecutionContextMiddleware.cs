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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;

public class ExecutionContextMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExecutionContextMiddleware> _logger;

    public ExecutionContextMiddleware(ILogger<ExecutionContextMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var executionContext = context.GetService<BuildingBlocks.Domain.ExecutionContext>();
        if (ExecutionType.TryFromName(context.FunctionDefinition.Name, out var executionType))
        {
            executionContext.SetExecutionType(executionType!);
        }
        else
        {
            _logger.LogWarning("Could not determine execution type for function {FunctionName}", context.FunctionDefinition.Name);
        }

        await next(context).ConfigureAwait(false);
    }
}
