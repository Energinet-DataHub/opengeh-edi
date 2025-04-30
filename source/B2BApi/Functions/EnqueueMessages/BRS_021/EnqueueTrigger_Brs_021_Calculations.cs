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

using System.Net;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NodaTime.Extensions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public class EnqueueTrigger_Brs_021_Calculations(
    ILogger<EnqueueTrigger_Brs_021_Calculations> logger,
    IOutgoingMessagesClient outgoingMessagesClient,
    UnitOfWork unitOfWork)
{
    private readonly ILogger<EnqueueTrigger_Brs_021_Calculations> _logger = logger;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly UnitOfWork _unitOfWork = unitOfWork;

    [Function(nameof(EnqueueTrigger_Brs_021_Calculations))]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "enqueue/brs021/calculations/{calculationTypeName}")]
        HttpRequestData request,
        FunctionContext executionContext,
        string? calculationTypeName,
        CancellationToken hostCancellationToken)
    {
        _logger.LogInformation("BRS-021 enqueue request for {calculationTypeName} received", calculationTypeName);

        var measurements = JsonSerializer.Deserialize<EnqueueMeasureDataSyncV1>(request.Body)!;

        var energyObservations = measurements.MeasureData
            .Select(x =>
                new EnergyObservationDto(
                    Position: x.Position,
                    Quantity: x.EnergyQuantity,
                    Quality: Quality.FromName(x.QuantityQuality.Name)))
            .ToList();

        // TODO: New data structure without `RelatedToMessageId` and `OriginalTransactionIdReferenceId`?
        var acceptedForwardMeteredDataMessageDto = new AcceptedForwardMeteredDataMessageDto(
            eventId:
            EventId.From(measurements.GetHashCode().ToString()), // TODO: Use the correct EventId and externalId
            externalId: new ExternalId(Guid.NewGuid()),
            receiver: new Actor(
                ActorNumber.Create(measurements.Receiver.ActorNumber),
                ActorRole.FromName(measurements.Receiver.ActorRole.Name)),
            businessReason: BusinessReason.PeriodicMetering,
            relatedToMessageId: MessageId.Create(Guid.NewGuid().ToString("N")), // TODO: Use the correct MessageId
            gridAreaCode: measurements.GridAreaCode,
            series: new ForwardMeteredDataMessageSeriesDto(
                TransactionId: TransactionId.New(),
                MarketEvaluationPointNumber: measurements.MeteringPointId,
                MarketEvaluationPointType: MeteringPointType.FromName(measurements.MeteringPointType.Name),
                OriginalTransactionIdReferenceId: null,
                Product: measurements.ProductNumber,
                QuantityMeasureUnit: MeasurementUnit.FromName(measurements.MeasureUnit.Name),
                RegistrationDateTime: measurements.RegistrationDateTime.ToInstant(),
                Resolution: Resolution.FromName(measurements.Resolution.Name),
                StartedDateTime: measurements.StartDateTime.ToInstant(),
                EndedDateTime: measurements.EndDateTime.ToInstant(),
                EnergyObservations: energyObservations));

        await _outgoingMessagesClient.EnqueueAsync(acceptedForwardMeteredDataMessageDto, CancellationToken.None)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync(hostCancellationToken).ConfigureAwait(false);

        return await Task.FromResult(request.CreateResponse(HttpStatusCode.OK)).ConfigureAwait(false);
    }
}
