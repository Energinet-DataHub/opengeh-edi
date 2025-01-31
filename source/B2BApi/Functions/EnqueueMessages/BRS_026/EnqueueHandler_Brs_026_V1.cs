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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using TimeSeriesType = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request.TimeSeriesType;

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
        Guid serviceBusMessageId,
        string orchestrationInstanceId,
        RequestCalculatedEnergyTimeSeriesAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 026. Data: {0}",
            acceptedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        // DUMMY for build purpose.
        var request = new AggregatedTimeSeriesRequest(
            Period: new EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Request.Period(Instant.FromUtc(2024, 1, 31, 23, 00), Instant.FromUtc(2024, 2, 1, 23, 00)),
            TimeSeriesTypes: [TimeSeriesType.FlexConsumption],
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
        // TODO: Add correct properties
        await _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(acceptedData.BusinessReason.ToString(), query).ConfigureAwait(false);

        // 3. See inside _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(query).

        // TODO: NotifyOrchestrationInstanceAsync should maybe happen in another layer, when the method is actually implemented
        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId,
                    RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        string orchestrationInstanceId,
        RequestCalculatedEnergyTimeSeriesRejectedV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 026. Data: {0}",
            rejectedData);

        var rejectReasons = rejectedData.ValidationErrors.Select(
                e => new RejectedEnergyResultMessageRejectReason(
                    ErrorCode: e.ErrorCode,
                    ErrorMessage: e.Message))
            .ToList();

        var rejectedTimeSeries = new RejectedEnergyResultMessageSerie(
            TransactionId: TransactionId.New(),
            RejectReasons: rejectReasons,
            OriginalTransactionIdReference: rejectedData.TransactionId);

        var enqueueRejectedMessageDto = new RejectedEnergyResultMessageDto(
            relatedToMessageId: rejectedData.OriginalMessageId,
            receiverNumber: rejectedData.RequestedByActorNumber,
            receiverRole: rejectedData.RequestedByActorRole,
            documentReceiverNumber: rejectedData.RequestedForActorNumber,
            documentReceiverRole: rejectedData.RequestedForActorRole,
            processId: Guid.Parse(orchestrationInstanceId), // TODO: Is this viable? Is the orchestration instance id always a guid?
            eventId: EventId.From(serviceBusMessageId),
            businessReason: rejectedData.BusinessReason,
            series: rejectedTimeSeries);

        await _actorRequestsClient.EnqueueRejectAggregatedMeasureDataRequestAsync(enqueueRejectedMessageDto, cancellationToken)
            .ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId,
                    RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }
}
