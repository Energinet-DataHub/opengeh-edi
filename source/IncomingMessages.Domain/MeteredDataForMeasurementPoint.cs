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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain;

public record MeteredDataForMeasurementPoint(
    string MessageId, //HeaderEnergyDocument.Identification
    string MessageType, //HeaderEnergyDocument.DocumentType
    string CreatedAt, //HeaderEnergyDocument.Creation
    string SenderNumber, //HeaderEnergyDocument.SenderEnergyParty.Identification
    string ReceiverNumber, //HeaderEnergyDocument.RecipientEnergyParty.Identification
    string SenderRoleCode, //ProcessEnergyContext.EnergyBusinessProcessRole
    string BusinessReason, //ProcessEnergyContext.EnergyBusinessProcess
    string ReceiverRoleCode, //Todo?
    string? BusinessType, //ProcessEnergyContext.EnergyIndustryClassification
    IReadOnlyCollection<IIncomingMessageSeries> Series) : IIncomingMessage;

public record MeteredDataForMeasurementPointSeries(
    string TransactionId, //PayloadEnergyTimeSeries.Identification
    string? Resolution, //PayloadEnergyTimeSeries.ObservationTimeSeriesPeriod.ResolutionDuration
    string StartDateTime, //PayloadEnergyTimeSeries.ObservationTimeSeriesPeriod.Start
    string? EndDateTime, //PayloadEnergyTimeSeries.ObservationTimeSeriesPeriod.End
    string? ProductNumber, //PayloadEnergyTimeSeries.IncludedProductCharacteristic.Identification
    string? ProductUnitType, //PayloadEnergyTimeSeries.IncludedProductCharacteristic.UnitType
    string? MeteringPointType, //PayloadEnergyTimeSeries.DetailMeasurementMeteringPointCharacteristic.TypeOfMeteringPoint
    string? MeteringPointLocationId, //PayloadEnergyTimeSeries.MeteringPointDomainLocation.Identification
    IReadOnlyCollection<EnergyObservation> EnergyObservations) : BaseDelegatedSeries, IIncomingMessageSeries
{
    // TODO: This is not used, but it is part of the interface. Will be removed in the future.
    public string? GridArea => null;

    public ActorNumber? GetActorNumberForRole(ActorRole actorRole, ActorNumber? gridAreaOwner)
    {
        throw new NotImplementedException();
    }
}

public record EnergyObservation(
    string? Position, //PayloadEnergyTimeSeries.IntervalEnergyObservation.Position
    string? EnergyQuantity, //PayloadEnergyTimeSeries.IntervalEnergyObservation.EnergyQuantity
    string? QuantityQuality); //PayloadEnergyTimeSeries.IntervalEnergyObservation.QuantityQuality
