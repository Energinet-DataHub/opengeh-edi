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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Microsoft.Extensions.Logging;
using Period = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-026 (RequestAggregatedMeasureData).
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_026_V1(
    ILogger<EnqueueHandler_Brs_026_V1> logger,
    IActorRequestsClient actorRequestsClient,
    IProcessManagerMessageClient processManagerMessageClient)
    : EnqueueActorMessagesValidatedHandlerBase<RequestCalculatedEnergyTimeSeriesAcceptedV1, RequestCalculatedEnergyTimeSeriesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IActorRequestsClient _actorRequestsClient = actorRequestsClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;

    protected override async Task EnqueueAcceptedMessagesAsync(
        string orchestrationInstanceId,
        RequestCalculatedEnergyTimeSeriesAcceptedV1 acceptedData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 026. Data: {0}",
            acceptedData);

        var request = AggregatedTimeSeriesRequestFactory.Parse(acceptedData);

        var query = new AggregatedTimeSeriesQueryParameters(
            request.TimeSeriesTypes.Select(CalculationTimeSeriesTypeMapper.MapTimeSeriesTypeFromEdi).ToList(),
            request.AggregationPerRoleAndGridArea.GridAreaCodes,
            request.AggregationPerRoleAndGridArea.EnergySupplierId,
            request.AggregationPerRoleAndGridArea.BalanceResponsibleId,
            request.CalculationType,
            new Period(request.Period.Start, request.Period.End));

        await _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(
            orchestrationInstanceId: orchestrationInstanceId,
            originalTransactionId: acceptedData.OriginalTransactionId,
            originalMessageId: acceptedData.OriginalMessageId,
            requestedForActorNumber: ActorNumber.Create(acceptedData.RequestedForActorNumber.Value),
            requestedForActorRole: ActorRole.FromCode(acceptedData.RequestedForActorRole.Code),
            businessReason: BusinessReason.FromCode(acceptedData.BusinessReason.Code),
            meteringPointType: acceptedData.MeteringPointType != null ? MeteringPointType.FromCode(acceptedData.MeteringPointType.Code) : null,
            settlementMethod: acceptedData.SettlementMethod != null ? SettlementMethod.FromCode(acceptedData.SettlementMethod.Code) : null,
            settlementVersion: acceptedData.SettlementVersion != null ? SettlementVersion.FromCode(acceptedData.SettlementVersion.Code) : null,
            query).ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId,
                    RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        string orchestrationInstanceId,
        RequestCalculatedEnergyTimeSeriesRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 026. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues rejected message

        // TODO: NotifyOrchestrationInstanceAsync should maybe happen in another layer, when the method is actually implemented
        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
            new NotifyOrchestrationInstanceEvent(
                OrchestrationInstanceId: orchestrationInstanceId,
                RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
            CancellationToken.None)
            .ConfigureAwait(false);
    }
}
