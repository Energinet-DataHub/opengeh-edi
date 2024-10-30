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
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeMeteredDataForMeasurementPointProcessDtoFactory
{
    public static InitializeMeteredDataForMeasurementPointMessageProcessDto Create(MeteredDataForMeasurementPointMessage meteredDataForMeasurementPointMessage)
    {
        ArgumentNullException.ThrowIfNull(meteredDataForMeasurementPointMessage);

        var senderActorNumber = ActorNumber.Create(meteredDataForMeasurementPointMessage.SenderNumber);
        var senderActorRole = ActorRole.FromCode(meteredDataForMeasurementPointMessage.SenderRoleCode);

        var series = meteredDataForMeasurementPointMessage.Series
            .Cast<MeteredDataForMeasurementPointSeries>()
            .Select(
                series =>
                {
                    return new InitializeMeteredDataForMeasurementPointMessageSeries(
                        TransactionId: series.TransactionId,
                        Resolution: series.Resolution,
                        StartDateTime: series.StartDateTime,
                        EndDateTime: series.EndDateTime,
                        ProductNumber: series.ProductNumber,
                        ProductUnitType: series.ProductUnitType,
                        MeteringPointType: series.MeteringPointType,
                        MeteringPointLocationId: series.MeteringPointLocationId,
                        RequestedByActor: RequestedByActor.From(
                            senderActorNumber,
                            series.RequestedByActorRole ?? senderActorRole),
                        OriginalActor: OriginalActor.From(
                            series.OriginalActorNumber ?? senderActorNumber,
                            senderActorRole),
                        EnergyObservations: series.EnergyObservations
                            .Select(
                                energyObservation => new InitializeEnergyObservation(
                                    Position: energyObservation.Position,
                                    EnergyQuantity: energyObservation.EnergyQuantity,
                                    QuantityQuality: energyObservation.QuantityQuality))
                            .ToList()
                            .AsReadOnly());
                })
            .ToList().AsReadOnly();

        return new InitializeMeteredDataForMeasurementPointMessageProcessDto(
            meteredDataForMeasurementPointMessage.MessageId,
            meteredDataForMeasurementPointMessage.MessageType,
            meteredDataForMeasurementPointMessage.CreatedAt,
            meteredDataForMeasurementPointMessage.BusinessReason,
            meteredDataForMeasurementPointMessage.BusinessType,
            series);
    }
}
