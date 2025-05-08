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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public class EnqueueTrigger_Brs_021_CalculatedMeasurements(
    ILogger<EnqueueTrigger_Brs_021_CalculatedMeasurements> logger,
    EnqueueHandler_Brs_021_CalculatedMeasurements_V1 handler)
{
    private readonly ILogger<EnqueueTrigger_Brs_021_CalculatedMeasurements> _logger = logger;
    private readonly EnqueueHandler_Brs_021_CalculatedMeasurements_V1 _handler = handler;

    [Authorize]
    [Function(nameof(EnqueueTrigger_Brs_021_CalculatedMeasurements))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = $"enqueue/{EnqueueCalculatedMeasurementsHttpV1.RouteName}")]
        HttpRequestData request,
        [FromBody] EnqueueCalculatedMeasurementsHttpV1 measurements,
        FunctionContext executionContext,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("BRS-021 enqueue request for calculated measurements received");

        await _handler.HandleAsync(measurements, cancellationToken).ConfigureAwait(false);

        return request.CreateResponse(HttpStatusCode.OK);
    }
}
