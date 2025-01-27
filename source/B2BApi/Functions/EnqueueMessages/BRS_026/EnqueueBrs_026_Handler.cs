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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-026 (RequestAggregatedMeasureData).
/// </summary>
/// <param name="logger"></param>
public class EnqueueBrs_026_Handler(
    ILogger<EnqueueBrs_026_Handler> logger,
    IActorRequestsClient actorRequestsClient,
    IProcessManagerMessageClient processManagerMessageClient)
    : EnqueueValidatedMessagesHandlerBase<RequestCalculatedEnergyTimeSeriesAcceptedV1, RequestCalculatedEnergyTimeSeriesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IActorRequestsClient _actorRequestsClient = actorRequestsClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;

    protected override async Task EnqueueAcceptedMessagesAsync(RequestCalculatedEnergyTimeSeriesAcceptedV1 acceptedData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 026. Data: {0}",
            acceptedData);

        // DUMMY for build purpose.
        var request = new AggregatedTimeSeriesRequest(
            Period: new EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request.Period(Instant.FromUtc(2024, 1, 31, 23, 00), Instant.FromUtc(2024, 2, 1, 23, 00)),
            TimeSeriesTypes: [EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request.TimeSeriesType.FlexConsumption],
            AggregationPerRoleAndGridArea: new AggregationPerRoleAndGridArea(["804"]),
            CalculationType: EDI.OutgoingMessages.Interfaces.Models.CalculationResults.CalculationType.Aggregation);

        // 1. Map RequestCalculatedEnergyTimeSeriesAcceptedV1 to AggregatedTimeSeriesQueryParameters when it is ready - model is currently empty.
        // DUMMY
        var query = new AggregatedTimeSeriesQueryParameters(
            [EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults.TimeSeriesType.FlexConsumption],
            ["804"],
            null,
            null,
            request.CalculationType,
            new EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period(request.Period.Start, request.Period.End));

        // 2. Call IActorRequestsClient.EnqueueAggregatedMeasureDataAsync(query + needed properties from RequestCalculatedEnergyTimeSeriesAcceptedV1);
        await _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(acceptedData.BusinessReason, query).ConfigureAwait(false);

        // 3. See inside _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(query).

        // 4. Notify ProcessManager
        var notifyEvent = new NotifyOrchestrationInstanceEvent("orchestrationInstanceId", RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(notifyEvent, CancellationToken.None).ConfigureAwait(false);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(RequestCalculatedEnergyTimeSeriesRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 026. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues rejected message
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
