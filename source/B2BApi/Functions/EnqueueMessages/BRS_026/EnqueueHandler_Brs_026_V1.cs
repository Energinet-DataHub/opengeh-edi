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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using SettlementMethod = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementMethod;

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

        var query = new AggregatedTimeSeriesQueryParameters(
            GetTimeSeriesTypes(acceptedData.MeteringPointType, acceptedData.SettlementMethod, acceptedData.RequestedForActorRole),
            acceptedData.GridAreas,
            acceptedData.EnergySupplierNumber?.Value,
            acceptedData.BalanceResponsibleNumber?.Value,
            MapCalculationType(acceptedData.BusinessReason.Name, acceptedData.SettlementVersion?.Name),
            new EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period(acceptedData.PeriodStart.ToInstant(), acceptedData.PeriodEnd.ToInstant()));

        var enqueuedCount = await _actorRequestsClient.EnqueueAggregatedMeasureDataAsync(
            eventId: EventId.From(serviceBusMessageId),
            orchestrationInstanceId: orchestrationInstanceId,
            originalMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
            originalTransactionId: TransactionId.From(acceptedData.OriginalTransactionId),
            requestedForActorNumber: ActorNumber.Create(acceptedData.RequestedForActorNumber),
            requestedForActorRole: ActorRole.Create(acceptedData.RequestedForActorRole),
            requestedByActorNumber: ActorNumber.Create(acceptedData.RequestedByActorNumber),
            requestedByActorRole: ActorRole.Create(acceptedData.RequestedByActorRole),
            businessReason: BusinessReason.FromName(acceptedData.BusinessReason.Name),
            meteringPointType: acceptedData.MeteringPointType != null ? MeteringPointType.FromName(acceptedData.MeteringPointType.Name) : null,
            settlementMethod: acceptedData.SettlementMethod != null ? SettlementMethod.FromName(acceptedData.SettlementMethod.Name) : null,
            settlementVersion: acceptedData.SettlementVersion != null ? SettlementVersion.FromName(acceptedData.SettlementVersion.Name) : null,
            query,
            cancellationToken).ConfigureAwait(false);

        if (enqueuedCount == 0)
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

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

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

    private static TimeSeriesType[] GetTimeSeriesTypes(
        ProcessManager.Components.Abstractions.ValueObjects.MeteringPointType? meteringPointType,
        ProcessManager.Components.Abstractions.ValueObjects.SettlementMethod? settlementMethod,
        ProcessManager.Components.Abstractions.ValueObjects.ActorRole requestedForActorRole)
    {
        return meteringPointType != null
            ? [MapTimeSeriesType(meteringPointType.Name, settlementMethod?.Name)]
            : requestedForActorRole.Name switch
            {
                DataHubNames.ActorRole.EnergySupplier =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                ],
                DataHubNames.ActorRole.BalanceResponsibleParty =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                ],
                DataHubNames.ActorRole.MeteredDataResponsible =>
                [
                    TimeSeriesType.Production,
                    TimeSeriesType.FlexConsumption,
                    TimeSeriesType.NonProfiledConsumption,
                    TimeSeriesType.TotalConsumption,
                    TimeSeriesType.NetExchangePerGa,
                ],
                _ => throw new ArgumentOutOfRangeException(
                    nameof(requestedForActorRole),
                    requestedForActorRole,
                    "Value does not contain a valid string representation of a requested by actor role."),
            };
    }

    private static CalculationType? MapCalculationType(string businessReason, string? settlementVersion)
    {
        if (businessReason == BusinessReason.Correction.Name && settlementVersion != null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settlementVersion),
                settlementVersion,
                $"Value must be null when {nameof(businessReason)} is not {nameof(BusinessReason.Correction.Name)}.");
        }

        return businessReason switch
        {
            _ when businessReason == BusinessReason.BalanceFixing.Name => CalculationType.BalanceFixing,
            _ when businessReason == BusinessReason.PreliminaryAggregation.Name => CalculationType.Aggregation,
            _ when businessReason == BusinessReason.WholesaleFixing.Name => CalculationType.WholesaleFixing,
            _ when businessReason == BusinessReason.Correction.Name => settlementVersion switch
            {
                DataHubNames.SettlementVersion.FirstCorrection => CalculationType.FirstCorrectionSettlement,
                DataHubNames.SettlementVersion.SecondCorrection => CalculationType.SecondCorrectionSettlement,
                DataHubNames.SettlementVersion.ThirdCorrection => CalculationType.ThirdCorrectionSettlement,
                null => null, // CalculationType == null means get latest correction
                _ => throw new ArgumentOutOfRangeException(
                    nameof(settlementVersion),
                    settlementVersion,
                    $"Value cannot be mapped to a {nameof(CalculationType)}."),
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(businessReason),
                businessReason,
                $"Value cannot be mapped to a {nameof(CalculationType)}."),
        };
    }

    private static TimeSeriesType MapTimeSeriesType(string meteringPointType, string? settlementMethod)
    {
        return meteringPointType switch
        {
            _ when meteringPointType == MeteringPointType.Production.Name => TimeSeriesType.Production,
            _ when meteringPointType == MeteringPointType.Exchange.Name => TimeSeriesType.NetExchangePerGa,
            _ when meteringPointType == MeteringPointType.Consumption.Name => settlementMethod switch
            {
                DataHubNames.SettlementMethod.NonProfiled => TimeSeriesType.NonProfiledConsumption,
                DataHubNames.SettlementMethod.Flex => TimeSeriesType.FlexConsumption,
                var method when
                    string.IsNullOrWhiteSpace(method) => TimeSeriesType.TotalConsumption,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(settlementMethod),
                    actualValue: settlementMethod,
                    "Value does not contain a valid string representation of a settlement method."),
            },

            _ => throw new ArgumentOutOfRangeException(
                nameof(meteringPointType),
                actualValue: meteringPointType,
                "Value does not contain a valid string representation of a metering point type."),
        };
    }
}
