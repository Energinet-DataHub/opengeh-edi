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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Mappers;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using ActorRole = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects.ActorRole;
using ChargeType = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ChargeType;
using Resolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-028 (RequestWholesaleServices).
/// </summary>
public class EnqueueHandler_Brs_028_V1(
    ILogger<EnqueueHandler_Brs_028_V1> logger,
    IActorRequestsClient actorRequestsClient,
    IProcessManagerMessageClient processManagerMessageClient,
    IUnitOfWork unitOfWork)
    : EnqueueActorMessagesValidatedHandlerBase<RequestCalculatedWholesaleServicesAcceptedV1, RequestCalculatedWholesaleServicesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IActorRequestsClient _actorRequestsClient = actorRequestsClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestCalculatedWholesaleServicesAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 028. Data: {0}",
            acceptedData);

        var settlementVersion = acceptedData.SettlementVersion is not null
            ? BuildingBlocks.Domain.Models.SettlementVersion.FromName(acceptedData.SettlementVersion.Name)
            : null;
        var queryParams = new WholesaleServicesQueryParameters(
            AmountType: AmountType.AmountPerCharge, // Default to AmountPerCharge, will be replaced in foreach loop
            GridAreaCodes: acceptedData.GridAreas,
            EnergySupplierId: acceptedData.EnergySupplierNumber?.Value,
            ChargeOwnerId: acceptedData.ChargeOwnerNumber?.Value,
            ChargeTypes: acceptedData.ChargeTypes.Select(
                    ct => (ct.ChargeCode, GetChargeType(ct.ChargeType)))
                .ToList(),
            BusinessReason: BuildingBlocks.Domain.Models.BusinessReason.FromName(acceptedData.BusinessReason.Name),
            SettlementVersion: settlementVersion,
            Period: new Period(
                acceptedData.PeriodStart.ToInstant(),
                acceptedData.PeriodEnd.ToInstant()),
            RequestedForEnergySupplier: acceptedData.RequestedForActorRole == ActorRole.EnergySupplier,
            RequestedForActorNumber: acceptedData.RequestedForActorNumber.Value);

        var amountTypes = AmountTypeMapper.Map(acceptedData.Resolution, acceptedData.ChargeTypes);

        var totalEnqueuedCount = 0;
        foreach (var amountType in amountTypes)
        {
            var queryParamsForAmountType = queryParams with
            {
                AmountType = amountType,
            };

            var enqueuedCount = await _actorRequestsClient.EnqueueWholesaleServicesAsync(
                    wholesaleServicesQueryParameters: queryParamsForAmountType,
                    requestedByActorNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(acceptedData.RequestedByActorNumber),
                    requestedByActorRole: BuildingBlocks.Domain.Models.ActorRole.Create(acceptedData.RequestedByActorRole),
                    requestedForActorNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(acceptedData.RequestedForActorNumber),
                    requestedForActorRole: BuildingBlocks.Domain.Models.ActorRole.Create(acceptedData.RequestedForActorRole),
                    orchestrationInstanceId: orchestrationInstanceId,
                    eventId: BuildingBlocks.Domain.Models.EventId.From(serviceBusMessageId),
                    originalMessageId: BuildingBlocks.Domain.Models.MessageId.Create(acceptedData.OriginalActorMessageId),
                    originalTransactionId: BuildingBlocks.Domain.Models.TransactionId.From(acceptedData.OriginalTransactionId),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            totalEnqueuedCount += enqueuedCount;
        }

        if (totalEnqueuedCount == 0)
        {
            _logger.LogInformation("No wholesale services messages enqueued for accepted BRS-028, enqueuing rejected message. Data: {0}", acceptedData);
            await _actorRequestsClient.EnqueueRejectWholesaleServicesRequestWithNoDataAsync(
                    queryParams,
                    requestedByActorNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(acceptedData.RequestedByActorNumber.Value),
                    requestedByActorRole: BuildingBlocks.Domain.Models.ActorRole.FromName(acceptedData.RequestedByActorRole.Name),
                    requestedForActorNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(acceptedData.RequestedForActorNumber.Value),
                    requestedForActorRole: BuildingBlocks.Domain.Models.ActorRole.FromName(acceptedData.RequestedForActorRole.Name),
                    orchestrationInstanceId: orchestrationInstanceId,
                    eventId: BuildingBlocks.Domain.Models.EventId.From(serviceBusMessageId),
                    originalMessageId: BuildingBlocks.Domain.Models.MessageId.Create(acceptedData.OriginalActorMessageId),
                    originalTransactionId: BuildingBlocks.Domain.Models.TransactionId.From(acceptedData.OriginalTransactionId),
                    businessReason: BuildingBlocks.Domain.Models.BusinessReason.FromName(acceptedData.BusinessReason.Name),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestCalculatedWholesaleServicesRejectedV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 028. Data: {0}",
            rejectedData);

        var rejectReasons = rejectedData.ValidationErrors.Select(
                e => new RejectedWholesaleServicesMessageRejectReason(
                    ErrorCode: e.ErrorCode,
                    ErrorMessage: e.Message))
            .ToList();

        var rejectedTimeSeries = new RejectedWholesaleServicesMessageSeries(
            TransactionId: BuildingBlocks.Domain.Models.TransactionId.New(),
            RejectReasons: rejectReasons,
            OriginalTransactionIdReference: BuildingBlocks.Domain.Models.TransactionId.From(rejectedData.OriginalTransactionId));

        var rejectedMessageDto = new RejectedWholesaleServicesMessageDto(
            relatedToMessageId: BuildingBlocks.Domain.Models.MessageId.Create(rejectedData.OriginalMessageId),
            receiverNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(rejectedData.RequestedByActorNumber.Value),
            receiverRole: BuildingBlocks.Domain.Models.ActorRole.FromName(rejectedData.RequestedByActorRole.Name),
            documentReceiverNumber: BuildingBlocks.Domain.Models.ActorNumber.Create(rejectedData.RequestedForActorNumber.Value),
            documentReceiverRole: BuildingBlocks.Domain.Models.ActorRole.FromName(rejectedData.RequestedForActorRole.Name),
            processId: orchestrationInstanceId,
            eventId: BuildingBlocks.Domain.Models.EventId.From(serviceBusMessageId),
            businessReason: BuildingBlocks.Domain.Models.BusinessReason.FromName(rejectedData.BusinessReason.Name).Name,
            series: rejectedTimeSeries);

        await _actorRequestsClient.EnqueueRejectWholesaleServicesRequestAsync(rejectedMessageDto, cancellationToken)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent(
                    OrchestrationInstanceId: orchestrationInstanceId.ToString(),
                    RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType? GetChargeType(
        ChargeType? chargeType)
    {
        if (chargeType is null)
            return null;

        if (chargeType == ChargeType.Subscription)
            return EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Subscription;
        if (chargeType == ChargeType.Fee)
            return EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Fee;
        if (chargeType == ChargeType.Tariff)
            return EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Tariff;

        throw new ArgumentOutOfRangeException(
            nameof(chargeType),
            chargeType,
            "Unknown charge type name");
    }
}
