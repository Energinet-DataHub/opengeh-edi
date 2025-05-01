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
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public class EnqueueTrigger_Brs_021_Calculations(
    ILogger<EnqueueTrigger_Brs_021_Calculations> logger,
    EnqueueHandler_Brs_021_Calculations_V1 handler)
{
    private readonly ILogger<EnqueueTrigger_Brs_021_Calculations> _logger = logger;
    private readonly EnqueueHandler_Brs_021_Calculations_V1 _handler = handler;

    [Function(nameof(EnqueueTrigger_Brs_021_Calculations))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "enqueue/brs021/calculations/{calculationTypeName}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string? calculationTypeName,
        CancellationToken hostCancellationToken)
    {
        _logger.LogInformation("BRS-021 enqueue request for {calculationTypeName} received", calculationTypeName);

        await _handler.HandleAsync(request, hostCancellationToken).ConfigureAwait(false);

        return await Task.FromResult(request.CreateResponse(HttpStatusCode.OK)).ConfigureAwait(false);
    }
}
