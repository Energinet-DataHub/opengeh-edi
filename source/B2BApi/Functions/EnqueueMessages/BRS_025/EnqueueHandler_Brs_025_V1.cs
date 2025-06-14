﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint.Request;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025.V1.Model;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NodaTime;
using NodaTime.Extensions;
using Polly;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_025;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-025 (RequestMeasurements).
/// </summary>
public class EnqueueHandler_Brs_025_V1(
    ILogger<EnqueueHandler_Brs_025_V1> logger,
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient,
    IUnitOfWork unitOfWork,
    IFeatureManager featureManager,
    IClock clock)
    : EnqueueActorMessagesValidatedHandlerBase<RequestMeasurementsAcceptedV1, RequestMeasurementsRejectV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IClock _clock = clock;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestMeasurementsAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 025. Data: {0}.",
            acceptedData);

        foreach (var measurement in acceptedData.Measurements)
        {
            var acceptedRequestMeasurementsMessageDto = GetAcceptedRequestMeasurementsMessageDto(
                serviceBusMessageId,
                orchestrationInstanceId,
                acceptedData,
                measurement);

            await EnqueueAcceptMessageAsync(acceptedRequestMeasurementsMessageDto, cancellationToken).ConfigureAwait(false);
        }

        if (await _featureManager.EnqueueRequestMeasurementsResponseMessagesAsync().ConfigureAwait(false))
        {
            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        await NotifyProcessManagerAsync(orchestrationInstanceId).ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        RequestMeasurementsRejectV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 025. Data: {0}.",
            rejectedData);

        var rejectRequestMeasurementsMessageDto = GetRejectRequestMeasurementsMessageDto(
            serviceBusMessageId,
            orchestrationInstanceId,
            orchestrationStartedByActor,
            rejectedData);

        if (await _featureManager.EnqueueRequestMeasurementsResponseMessagesAsync().ConfigureAwait(false))
        {
            await EnqueueRejectMessageAsync(rejectRequestMeasurementsMessageDto, cancellationToken).ConfigureAwait(false);
        }

        await NotifyProcessManagerAsync(orchestrationInstanceId).ConfigureAwait(false);
    }

    private static RejectRequestMeasurementsMessageDto GetRejectRequestMeasurementsMessageDto(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        RequestMeasurementsRejectV1 rejectedData)
    {
        var rejectReasons = rejectedData.ValidationErrors.Select(
                e => new RejectRequestMeasurementsMessageRejectReason(
                    ErrorCode: e.ErrorCode,
                    ErrorMessage: e.Message))
            .ToList();

        var rejectedTimeSeries = new RejectRequestMeasurementsMessageSeries(
            TransactionId: TransactionId.New(),
            MeteringPointId: MeteringPointId.From(rejectedData.MeteringPointId),
            RejectReasons: rejectReasons,
            OriginalTransactionIdReference: TransactionId.From(rejectedData.OriginalTransactionId));

        return new RejectRequestMeasurementsMessageDto(
            relatedToMessageId: MessageId.Create(rejectedData.OriginalActorMessageId),
            receiverNumber: ActorNumber.Create(orchestrationStartedByActor.ActorNumber),
            receiverRole: ActorRole.FromName(orchestrationStartedByActor.ActorRole.ToString()),
            documentReceiverRole: ActorRole.FromName(rejectedData.ActorRole.Name),
            documentReceiverNumber: ActorNumber.Create(rejectedData.ActorNumber.Value),
            processId: orchestrationInstanceId,
            eventId: EventId.From(serviceBusMessageId),
            businessReason: BusinessReason.PeriodicMetering,
            series: rejectedTimeSeries);
    }

    private AcceptedRequestMeasurementsMessageDto GetAcceptedRequestMeasurementsMessageDto(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestMeasurementsAcceptedV1 acceptedData,
        Measurement measurement)
    {
        var measurementDtos = new List<MeasurementDto>();
        foreach (var measurementPoint in measurement.MeasurementPoints)
        {
            var measurementDto = new MeasurementDto(
                Position: measurementPoint.Position,
                Quantity: measurementPoint.EnergyQuantity,
                Quality: Quality.FromName(measurementPoint.QuantityQuality.Name));

            measurementDtos.Add(measurementDto);
        }

        return new AcceptedRequestMeasurementsMessageDto(
            eventId: EventId.From(serviceBusMessageId),
            externalId: new ExternalId(orchestrationInstanceId),
            receiver: new Actor(ActorNumber.Create(acceptedData.ActorNumber), ActorRole.FromName(acceptedData.ActorRole.Name)),
            businessReason: BusinessReason.PeriodicMetering,
            relatedToMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
            series: new MeasurementsDto(
                TransactionId: TransactionId.New(),
                MeteringPointId: acceptedData.MeteringPointId,
                MeteringPointType: MeteringPointType.FromName(acceptedData.MeteringPointType.Name),
                OriginalTransactionIdReference: TransactionId.From(acceptedData.OriginalTransactionId),
                Product: ProductType.EnergyActive.Code,
                MeasurementUnit: MeasurementUnit.KilowattHour,
                RegistrationDateTime: _clock.GetCurrentInstant(),
                Resolution: Resolution.FromName(measurement.Resolution.Name),
                Period: new Period(measurement.StartDateTime.ToInstant(), measurement.EndDateTime.ToInstant()),
                Measurements: measurementDtos));
    }

    private async Task EnqueueAcceptMessageAsync(
        AcceptedRequestMeasurementsMessageDto acceptedRequestMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        await _outgoingMessagesClient.EnqueueAsync(acceptedRequestMeasurementsMessageDto, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task NotifyProcessManagerAsync(Guid orchestrationInstanceId)
    {
        var executionPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)]);

        await executionPolicy.ExecuteAsync(
            () => _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new RequestMeasurementsNotifyEventV1(orchestrationInstanceId.ToString()),
                CancellationToken.None)).ConfigureAwait(false);
    }

    private async Task EnqueueRejectMessageAsync(
        RejectRequestMeasurementsMessageDto rejectRequestMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        await _outgoingMessagesClient.EnqueueAsync(rejectRequestMeasurementsMessageDto, CancellationToken.None)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
    }
}
