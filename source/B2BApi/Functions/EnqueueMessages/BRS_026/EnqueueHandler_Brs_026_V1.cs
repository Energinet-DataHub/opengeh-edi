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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-026 (RequestAggregatedMeasureData).
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_026_V1(
    ILogger<EnqueueHandler_Brs_026_V1> logger,
    IActorRequestsClient actorRequestsClient,
    IProcessManagerMessageClient processManagerMessageClient,
    IUnitOfWork unitOfWork)
    : EnqueueActorMessagesValidatedHandlerBase<RequestCalculatedEnergyTimeSeriesAcceptedV1, RequestCalculatedEnergyTimeSeriesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IActorRequestsClient _actorRequestsClient = actorRequestsClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestCalculatedEnergyTimeSeriesAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 026. Data: {0}",
            acceptedData);

        var totalEnqueuedCount = 0;
        var request = AggregatedTimeSeriesRequestFactory.Parse(acceptedData);

        var query = new AggregatedTimeSeriesQueryParameters(
            request.TimeSeriesTypes.Select(CalculationTimeSeriesTypeMapper.MapTimeSeriesType).ToList(),
            request.AggregationPerRoleAndGridArea.GridAreaCodes,
            request.AggregationPerRoleAndGridArea.EnergySupplierId,
            request.AggregationPerRoleAndGridArea.BalanceResponsibleId,
            request.CalculationType,
            new EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period(request.Period.Start, request.Period.End));

        var enqueuedCount = await _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(
            serviceBusMessageId: serviceBusMessageId,
            orchestrationInstanceId: orchestrationInstanceId,
            originalMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
            originalTransactionId: TransactionId.From(acceptedData.OriginalTransactionId),
            requestedForActorNumber: ActorNumber.Create(acceptedData.RequestedForActorNumber.Value),
            requestedForActorRole: ActorRole.FromName(acceptedData.RequestedForActorRole.Name),
            requestedByActorNumber: ActorNumber.Create(acceptedData.RequestedByActorNumber.Value),
            requestedByActorRole: ActorRole.FromName(acceptedData.RequestedByActorRole.Name),
            businessReason: BusinessReason.FromName(acceptedData.BusinessReason.Name),
            meteringPointType: acceptedData.MeteringPointType != null ? MeteringPointType.FromName(acceptedData.MeteringPointType.Name) : null,
            settlementMethod: acceptedData.SettlementMethod != null ? SettlementMethod.FromName(acceptedData.SettlementMethod.Name) : null,
            settlementVersion: acceptedData.SettlementVersion != null ? SettlementVersion.FromName(acceptedData.SettlementVersion.Name) : null,
            query,
            cancellationToken).ConfigureAwait(false);

        totalEnqueuedCount += enqueuedCount;

        if (totalEnqueuedCount == 0)
        {
            _logger.LogInformation("No aggregated measure data messages enqueued for accepted BRS-026, enqueuing rejected message. Data: {0}", acceptedData);
            await _actorRequestsClient.EnqueueRejectAggregatedMeasureDataRequestWithNoDataAsync(
                orchestrationInstanceId: orchestrationInstanceId,
                originalMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
                eventId: EventId.From(serviceBusMessageId),
                originalTransactionId: TransactionId.From(acceptedData.OriginalTransactionId),
                requestedByActorNumber: ActorNumber.Create(acceptedData.RequestedByActorNumber.Value),
                requestedByActorRole: ActorRole.FromName(acceptedData.RequestedByActorRole.Name),
                requestedForActorNumber: ActorNumber.Create(acceptedData.RequestedForActorNumber.Value),
                requestedForActorRole: ActorRole.FromName(acceptedData.RequestedForActorRole.Name),
                businessReason: BusinessReason.FromName(acceptedData.BusinessReason.Name),
                query,
                cancellationToken)
                .ConfigureAwait(false);
        }

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
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
            OriginalTransactionIdReference: TransactionId.From(rejectedData.OriginalTransactionId));

        var enqueueRejectedMessageDto = new RejectedEnergyResultMessageDto(
            relatedToMessageId: MessageId.Create(rejectedData.OriginalMessageId),
            receiverNumber: ActorNumber.Create(rejectedData.RequestedByActorNumber.Value),
            receiverRole: ActorRole.FromName(rejectedData.RequestedByActorRole.Name),
            documentReceiverNumber: ActorNumber.Create(rejectedData.RequestedForActorNumber.Value),
            documentReceiverRole: ActorRole.FromName(rejectedData.RequestedForActorRole.Name),
            processId: orchestrationInstanceId,
            eventId: EventId.From(serviceBusMessageId),
            businessReason: BusinessReason.FromName(rejectedData.BusinessReason.Name).Name,
            series: rejectedTimeSeries);

        await _actorRequestsClient.EnqueueRejectAggregatedMeasureDataRequestAsync(enqueueRejectedMessageDto, cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }
}
