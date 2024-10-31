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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.Process.Interfaces;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeMeteredDataForMeasurementPointProcessDtoFactory
{
    public static InitializeMeteredDataForMeasurementPointMessageProcessDto Create(MeteredDataForMeasurementPointMessage meteredDataForMeasurementPointMessage, AuthenticatedActor authenticatedActor)
    {
        ArgumentNullException.ThrowIfNull(meteredDataForMeasurementPointMessage);

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
                        DelegatedGridAreaCodes: series.DelegatedGridAreas,
                        RequestedByActor: RequestedByActor.From(
                            authenticatedActor.CurrentActorIdentity.ActorNumber,
                            series.RequestedByActorRole ?? authenticatedActor.CurrentActorIdentity.ActorRole),
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
