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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using Polly;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Readability")]
public sealed class EnqueueHandler_Brs_021_ForwardMeteredData_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient,
    IUnitOfWork unitOfWork,
    ILogger<EnqueueHandler_Brs_021_ForwardMeteredData_V1> logger)
    : EnqueueActorMessagesValidatedHandlerBase<ForwardMeteredDataAcceptedV1, ForwardMeteredDataRejectedV1>(logger)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        ForwardMeteredDataAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        foreach (var receivers in acceptedData.ReceiversWithMeteredData)
        {
            var energyObservations = receivers.MeteredData
                .Select(x =>
                    new MeasurementDto(
                        Position: x.Position,
                        Quantity: x.EnergyQuantity,
                        Quality: x.QuantityQuality != null ? Quality.FromName(x.QuantityQuality.Name) : null))
                .ToList();

            foreach (var actor in receivers.Actors)
            {
                var acceptedForwardMeteredDataMessageDto = new AcceptedSendMeasurementsMessageDto(
                    eventId: EventId.From(serviceBusMessageId),
                    externalId: new ExternalId(orchestrationInstanceId),
                    receiver: new Actor(ActorNumber.Create(actor.ActorNumber), ActorRole.FromName(actor.ActorRole.Name)),
                    businessReason: BusinessReason.PeriodicMetering,
                    relatedToMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
                    series: new MeasurementsDto(
                        TransactionId: TransactionId.New(),
                        MeteringPointId: acceptedData.MeteringPointId,
                        MeteringPointType: MeteringPointType.FromName(acceptedData.MeteringPointType.Name),
                        OriginalTransactionIdReference: null,
                        Product: acceptedData.ProductNumber,
                        MeasurementUnit: MeasurementUnit.FromName(receivers.MeasureUnit.Name),
                        RegistrationDateTime: acceptedData.RegistrationDateTime.ToInstant(),
                        Resolution: Resolution.FromName(receivers.Resolution.Name),
                        Period: new Period(receivers.StartDateTime.ToInstant(), receivers.EndDateTime.ToInstant()),
                        Measurements: energyObservations));

                await _outgoingMessagesClient.EnqueueAsync(acceptedForwardMeteredDataMessageDto, CancellationToken.None).ConfigureAwait(false);
            }
        }

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

        var executionPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)]);

        await executionPolicy.ExecuteAsync(
            () => _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new ForwardMeteredDataNotifyEventV1(orchestrationInstanceId.ToString()),
                CancellationToken.None)).ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesActorV1 orchestrationStartedByActor,
        ForwardMeteredDataRejectedV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 021. Data: {0}",
            rejectedData);

        var rejectedForwardMeteredDataMessageDto = new RejectedSendMeasurementsMessageDto(
            eventId: EventId.From(serviceBusMessageId),
            externalId: new ExternalId(serviceBusMessageId),
            businessReason: BusinessReason.FromName(rejectedData.BusinessReason.Name),
            receiverNumber: ActorNumber.Create(orchestrationStartedByActor.ActorNumber),
            receiverRole: ActorRole.FromName(orchestrationStartedByActor.ActorRole.ToString()),
            documentReceiverRole: ActorRole.FromName(rejectedData.ForwardedForActorRole.Name),
            relatedToMessageId: MessageId.Create(rejectedData.OriginalActorMessageId),
            meteringPointId: MeteringPointId.From(rejectedData.MeteringPointId),
            series: new RejectedForwardMeteredDataSeries(
                OriginalTransactionIdReference: TransactionId.From(rejectedData.OriginalTransactionId),
                TransactionId: TransactionId.New(),
                RejectReasons: rejectedData.ValidationErrors.Select(
                        validationError =>
                            new RejectReason(
                                ErrorCode: validationError.ErrorCode,
                                ErrorMessage: validationError.Message))
                    .ToList()));

        await _outgoingMessagesClient.EnqueueAsync(rejectedForwardMeteredDataMessageDto, CancellationToken.None)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

        var executionPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(
                [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)]);

        await executionPolicy.ExecuteAsync(
            () => _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new ForwardMeteredDataNotifyEventV1(orchestrationInstanceId.ToString()),
                CancellationToken.None)).ConfigureAwait(false);
    }
}
