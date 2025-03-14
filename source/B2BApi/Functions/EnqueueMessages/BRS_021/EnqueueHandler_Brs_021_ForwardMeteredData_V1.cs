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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using Polly;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public sealed class EnqueueHandler_Brs_021_ForwardMeteredData_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient,
    ILogger<EnqueueHandler_Brs_021_ForwardMeteredData_V1> logger)
    : EnqueueActorMessagesValidatedHandlerBase<ForwardMeteredDataAcceptedV1, ForwardMeteredDataRejectedV1>(logger)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        ForwardMeteredDataAcceptedV1 acceptedData,
        CancellationToken cancellationToken)
    {
        var series = acceptedData.AcceptedEnergyObservations.Select(x =>
            new EnergyObservationDto(x.Position, x.EnergyQuantity, x.QuantityQuality != null ? Quality.FromName(x.QuantityQuality.Name) : null))
            .ToList();

        foreach (var acceptedDataMarketActorRecipient in acceptedData.MarketActorRecipients)
        {
            var acceptedForwardMeteredDataMessageDto = new AcceptedForwardMeteredDataMessageDto(
                eventId: EventId.From(serviceBusMessageId),
                externalId: new ExternalId(orchestrationInstanceId),
                receiver: new Actor(ActorNumber.Create(acceptedDataMarketActorRecipient.ActorNumber), ActorRole.FromName(acceptedDataMarketActorRecipient.ActorRole.Name)),
                businessReason: BusinessReason.PeriodicMetering,
                relatedToMessageId: MessageId.Create(acceptedData.OriginalActorMessageId),
                series: new ForwardMeteredDataMessageSeriesDto(
                    TransactionId: TransactionId.New(),
                    MarketEvaluationPointNumber: acceptedData.MeteringPointId,
                    MarketEvaluationPointType: MeteringPointType.FromName(acceptedData.MeteringPointType.Name),
                    OriginalTransactionIdReferenceId: TransactionId.From(acceptedData.OriginalTransactionId),
                    Product: acceptedData.ProductNumber,
                    QuantityMeasureUnit: MeasurementUnit.FromName(acceptedData.MeasureUnit.Name),
                    RegistrationDateTime: acceptedData.RegistrationDateTime.ToInstant(),
                    Resolution: Resolution.FromName(acceptedData.Resolution.Name),
                    StartedDateTime: acceptedData.StartDateTime.ToInstant(),
                    EndedDateTime: acceptedData.EndDateTime.ToInstant(),
                    EnergyObservations: series));

            await _outgoingMessagesClient.EnqueueAndCommitAsync(acceptedForwardMeteredDataMessageDto, CancellationToken.None).ConfigureAwait(false);
        }

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
        ForwardMeteredDataRejectedV1 rejectedData,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 021. Data: {0}",
            rejectedData);

        // TODO: Fix when we have the correct data from ForwardMeteredDataRejectedV1
        var rejectedForwardMeteredDataMessageDto = new RejectedForwardMeteredDataMessageDto(
            EventId: EventId.From(serviceBusMessageId),
            BusinessReason: BusinessReason.PeriodicMetering,
            ReceiverId: ActorNumber.Create(rejectedData.ForwardedByActorNumber),
            ReceiverRole: ActorRole.Create(rejectedData.ForwardedByActorRole),
            ProcessId: orchestrationInstanceId,
            ExternalId: Guid.NewGuid(),
            AcknowledgementDto: new AcknowledgementDto(
                ReceivedMarketDocumentCreatedDateTime: null,
                ReceivedMarketDocumentTransactionId: null,
                ReceivedMarketDocumentProcessProcessType: null,
                ReceivedMarketDocumentRevisionNumber: null,
                ReceivedMarketDocumentTitle: null,
                ReceivedMarketDocumentType: null,
                Reason: [],
                InErrorPeriod: [],
                Series: rejectedData.ValidationErrors.Select(
                        validationError => new SeriesDto(
                            MRID: Guid.NewGuid().ToString(),
                            Reason:
                            [
                                new ReasonDto(
                                    Code: validationError.ErrorCode,
                                    Text: validationError.Message)
                            ]))
                    .ToList(),
                OriginalMktActivityRecord: [],
                RejectedTimeSeries: []));

        await _outgoingMessagesClient.EnqueueAndCommitAsync(rejectedForwardMeteredDataMessageDto, CancellationToken.None).ConfigureAwait(false);

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
