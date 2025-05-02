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

using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using Microsoft.Azure.Functions.Worker.Http;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public class EnqueueHandler_Brs_021_Calculations_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    IUnitOfWork unitOfWork)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task HandleAsync(HttpRequestData request, CancellationToken hostCancellationToken)
    {
        var requestBodyStream = request.Body;
        using var reader = new StreamReader(requestBodyStream);
        var requestBody = await reader.ReadToEndAsync(hostCancellationToken).ConfigureAwait(false);
        var measurements = JsonSerializer.Deserialize<EnqueueCalculatedMeasurementsHttpV1>(requestBody)!;

        var energyObservations = measurements.MeasureData
            .Select(x =>
                new EnergyObservationDto(
                    Position: x.Position,
                    Quantity: x.EnergyQuantity,
                    Quality: Quality.FromName(x.QuantityQuality.Name)))
            .ToList();

        var acceptedForwardMeteredDataMessageDto = new CalculatedMeteredDataMessageDto(
            eventId: EventId.From(new Random(requestBody.GetHashCode()).NextInt64().ToString()),
            externalId: new ExternalId(Guid.NewGuid()),
            receiver: new Actor(
                ActorNumber.Create(measurements.Receiver.ActorNumber),
                ActorRole.FromName(measurements.Receiver.ActorRole.Name)),
            businessReason: BusinessReason.PeriodicMetering,
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
    }
}
