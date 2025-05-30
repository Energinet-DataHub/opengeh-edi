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
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NodaTime.Extensions;
using Polly;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using MeteringPointType = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MeteringPointType;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_024;

/// <summary>
/// Enqueue accepted/rejected messages for BRS-024 (RequestMeasurements).
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_024_V1(
    ILogger<EnqueueHandler_Brs_024_V1> logger,
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient,
    IUnitOfWork unitOfWork,
    IFeatureManager featureManager)
    : EnqueueActorMessagesValidatedHandlerBase<RequestYearlyMeasurementsAcceptedV1, RequestYearlyMeasurementsRejectV1>(logger)
{
    private readonly ILogger _logger = logger;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFeatureManager _featureManager = featureManager;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        RequestYearlyMeasurementsAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 024. Data: {0}",
            acceptedData);

        var energyObservations = acceptedData.Measurements
            .Select(x =>
                new MeasurementDto(
                    Position: x.Position,
                    Quantity: x.EnergyQuantity,
                    Quality: Quality.FromName(x.QuantityQuality.Name)))
            .ToList();

        var acceptedRequestMeasurementsMessageDto = new AcceptedSendMeasurementsMessageDto(
            eventId: EventId.From(serviceBusMessageId),
            externalId: new ExternalId(orchestrationInstanceId),
            receiver: new Actor(ActorNumber.Create(acceptedData.ActorNumber), ActorRole.FromName(acceptedData.ActorRole.Name)),
            businessReason: BusinessReason.PeriodicMetering,
            relatedToMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
            gridAreaCode: acceptedData.GridAreaCode,
            series: new MeasurementsDto(
                TransactionId: TransactionId.New(),
                MeteringPointId: acceptedData.MeteringPointId,
                MeteringPointType: MeteringPointType.FromName(acceptedData.MeteringPointType.Name),
                OriginalTransactionIdReference: TransactionId.From(acceptedData.OriginalTransactionId),
                Product: acceptedData.ProductNumber,
                MeasurementUnit: MeasurementUnit.FromName(acceptedData.MeasureUnit.Name),
                RegistrationDateTime: acceptedData.RegistrationDateTime.ToInstant(),
                Resolution: Resolution.FromName(acceptedData.Resolution.Name),
                Period: new Period(acceptedData.StartDateTime.ToInstant(), acceptedData.EndDateTime.ToInstant()),
                Measurements: energyObservations));

        if (await _featureManager.EnqueueRequestMeasurementsResponseMessagesAsync().ConfigureAwait(false))
        {
            await _outgoingMessagesClient.EnqueueAsync(acceptedRequestMeasurementsMessageDto, CancellationToken.None)
                .ConfigureAwait(false);

            await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        var executionPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)]);

        await executionPolicy.ExecuteAsync(
            () => _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new RequestYearlyMeasurementsNotifyEventV1(orchestrationInstanceId.ToString()),
                CancellationToken.None)).ConfigureAwait(false);
    }

    protected override Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        RequestYearlyMeasurementsRejectV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 024. Data: {0}",
            rejectedData);

        throw new NotImplementedException();
    }
}
