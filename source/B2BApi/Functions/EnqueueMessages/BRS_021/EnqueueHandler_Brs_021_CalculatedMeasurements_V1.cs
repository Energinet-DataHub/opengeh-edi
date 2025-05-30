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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.Shared.V1.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;

public class EnqueueHandler_Brs_021_CalculatedMeasurements_V1(
    IOutgoingMessagesClient outgoingMessagesClient,
    IUnitOfWork unitOfWork)
{
    private readonly IOutgoingMessagesClient _outgoingMessagesClient = outgoingMessagesClient;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task HandleAsync(
        EnqueueCalculatedMeasurementsHttpV1 measurements,
        CancellationToken hostCancellationToken)
    {
        foreach (var receiversWithMeasurements in measurements.Data)
        {
            foreach (var receiver in receiversWithMeasurements.Receivers)
            {
                var actualMeasurements = receiversWithMeasurements.Measurements
                    .Select(x =>
                        new MeasurementDto(
                            Position: x.Position,
                            Quantity: x.EnergyQuantity,
                            Quality: Quality.FromName(x.QuantityQuality.Name)))
                    .ToList();

                var calculatedMeasurementsMessageDto = new CalculatedMeasurementsMessageDto(
                    eventId: EventId.From(Guid.CreateVersion7()),
                    externalId: new ExternalId(measurements.TransactionId),
                    receiver: new Actor(
                        ActorNumber.Create(receiver.ActorNumber),
                        ActorRole.FromName(receiver.ActorRole.Name)),
                    businessReason: BusinessReason.PeriodicMetering,
                    gridAreaCode: receiversWithMeasurements.GridAreaCode,
                    series: new MeasurementsDto(
                        TransactionId: TransactionId.New(),
                        MeteringPointId: measurements.MeteringPointId,
                        MeteringPointType: MeteringPointType.FromName(measurements.MeteringPointType.Name),
                        OriginalTransactionIdReference: null,
                        Product: ProductType.EnergyActive.Code,
                        MeasurementUnit: MeasurementUnit.FromName(measurements.MeasureUnit.Name),
                        RegistrationDateTime: receiversWithMeasurements.RegistrationDateTime.ToInstant(),
                        Resolution: Resolution.FromName(measurements.Resolution.Name),
                        Period: new Period(
                            receiversWithMeasurements.StartDateTime.ToInstant(),
                            receiversWithMeasurements.EndDateTime.ToInstant()),
                        Measurements: actualMeasurements));

                await _outgoingMessagesClient.EnqueueAsync(calculatedMeasurementsMessageDto, hostCancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await _unitOfWork.CommitTransactionAsync(hostCancellationToken).ConfigureAwait(false);
    }
}
