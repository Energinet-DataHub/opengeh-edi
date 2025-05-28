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

using System.Net;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_045.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_045;

public class EnqueueTrigger_Brs_045_MissingMeasurementsLog(
    ILogger<EnqueueTrigger_Brs_045_MissingMeasurementsLog> logger,
    EnqueueHandler_Brs_045_MissingMeasurementsLog enqueueHandler)
{
    private readonly ILogger<EnqueueTrigger_Brs_045_MissingMeasurementsLog> _logger = logger;
    private readonly EnqueueHandler_Brs_045_MissingMeasurementsLog _enqueueHandler = enqueueHandler;

    [Authorize]
    [Function(nameof(EnqueueTrigger_Brs_045_MissingMeasurementsLog))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = $"enqueue/{EnqueueMissingMeasurementsLogHttpV1.RouteName}")]
        HttpRequestData request,
        [FromBody] EnqueueMissingMeasurementsLogHttpV1 missingMeasurementsLog,
        FunctionContext executionContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("BRS-045 enqueue missing measurements log received");

        await _enqueueHandler.HandleAsync(missingMeasurementsLog, cancellationToken).ConfigureAwait(false);

        return request.CreateResponse(HttpStatusCode.OK);
    }
}
