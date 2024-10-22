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

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;

public sealed class SuppressOperationCanceledExceptionMiddleware(ILogger<UnHandledExceptionMiddleware> logger) : IFunctionsWorkerMiddleware
{
    private readonly ILogger<UnHandledExceptionMiddleware> _logger = logger;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            await next(context);
        }
        catch (OperationCanceledException operationCanceledException)
        {
            // This catch block handles cancellations triggered by a CancellationToken.
            // E.g. if a task is cancelled it throws a TaskCanceledException which is a OperationCanceledException.
            // E.g. if cancellationToken.ThrowIfCancellationRequested() it throws an task OperationCanceledException.
            // It logs a warning message indicating that the request was cancelled.
            _logger.LogWarning(operationCanceledException, "Request was cancelled: {Ex}", operationCanceledException.Message);
        }
    }
}
