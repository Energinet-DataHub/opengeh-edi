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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;

public static class InitializeMeteredDataForMeteringPointProcessDtoFactory
{
    public static InitializeMeteredDataForMeteringPointMessageProcessDto Create(MeteredDataForMeteringPointMessageBase meteredDataForMeteringPointMessageBase, AuthenticatedActor authenticatedActor)
    {
        ArgumentNullException.ThrowIfNull(meteredDataForMeteringPointMessageBase);

        var series = meteredDataForMeteringPointMessageBase.Series
            .Cast<MeteredDataForMeteringPointSeries>()
            .Select(
                series =>
                {
                    return new InitializeMeteredDataForMeteringPointMessageSeries(
                        TransactionId: series.TransactionId,
                        Resolution: series.Resolution,
                        StartDateTime: series.StartDateTime,
                        EndDateTime: series.EndDateTime,
                        ProductNumber: series.ProductNumber,
                        ProductUnitType: series.ProductUnitType,
                        MeteringPointType: series.MeteringPointType,
                        MeteringPointLocationId: series.MeteringPointLocationId,
                        RegisteredAt: series.RegisteredAt,
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

        return new InitializeMeteredDataForMeteringPointMessageProcessDto(
            meteredDataForMeteringPointMessageBase.MessageId,
            meteredDataForMeteringPointMessageBase.MessageType,
            meteredDataForMeteringPointMessageBase.CreatedAt,
            meteredDataForMeteringPointMessageBase.BusinessReason,
            meteredDataForMeteringPointMessageBase.BusinessType,
            series);
    }
}
