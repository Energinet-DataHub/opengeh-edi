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
using NodaTime.Extensions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public sealed class EnqueueHandler_Brs_021_Forward_Metered_Data_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    IProcessManagerMessageClient processManagerMessageClient,
    ILogger<EnqueueHandler_Brs_021_Forward_Metered_Data_V1> logger)
    : EnqueueActorMessagesValidatedHandlerBase<MeteredDataForMeteringPointAcceptedV1, MeteredDataForMeteringPointRejectedV1>(logger)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(string orchestrationInstanceId, MeteredDataForMeteringPointAcceptedV1 acceptedData)
    {
        var series = acceptedData.AcceptedEnergyObservations.Select(x =>
            new EnergyObservationDto(x.Position, x.EnergyQuantity, x.QuantityQuality?.Name))
            .ToList();

        foreach (var acceptedDataMarketActorRecipient in acceptedData.MarketActorRecipients)
        {
            var meteredDataForMeteringPointMessageProcessDto = new MeteredDataForMeteringPointMessageProcessDto(
                eventId: EventId.From(Guid.NewGuid()), // Should be exposed from PM as the external provider
                receiver: new Actor(ActorNumber.Create(acceptedDataMarketActorRecipient.ActorId), ActorRole.FromName(acceptedDataMarketActorRecipient.ActorRole.Name)),
                businessReason: BusinessReason.PeriodicMetering, // Should this be exposed from PM? Or should it always be PeriodicMetering and be set on the MeteredDataForMeteringPointMessageProcessDto ctor?
                relatedToMessageId: MessageId.Create(Guid.NewGuid().ToString()), // Should come from the incoming message, but we don't have that yet
                series: new MeteredDataForMeteringPointMessageSeriesDto(
                    TransactionId: TransactionId.New(),
                    MarketEvaluationPointNumber: acceptedData.MeteringPointId,
                    MarketEvaluationPointType: acceptedData.MeteringPointType.Name,
                    OriginalTransactionIdReferenceId: TransactionId.From(acceptedData.OriginalTransactionId),
                    Product: acceptedData.ProductNumber,
                    QuantityMeasureUnit: MeasurementUnit.FromName(acceptedData.MeasureUnit.Name),
                    RegistrationDateTime: acceptedData.RegistrationDateTime,
                    Resolution: Resolution.FromName(acceptedData.Resolution.Name),
                    StartedDateTime: acceptedData.StartDateTime.ToInstant(),
                    EndedDateTime: acceptedData.EndDateTime.ToInstant(),
                    EnergyObservations: series));

            await _outgoingMessagesClient.EnqueueAndCommitAsync(meteredDataForMeteringPointMessageProcessDto, CancellationToken.None).ConfigureAwait(false);
        }
        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(new NotifyOrchestrationInstanceEvent())
    }

    protected override async Task EnqueueRejectedMessagesAsync(string orchestrationInstanceId, MeteredDataForMeteringPointRejectedV1 rejectedData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 021. Data: {0}",
            rejectedData);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
