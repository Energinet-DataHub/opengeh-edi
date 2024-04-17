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

using System;
using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class AcceptedEnergyResultMessageDto : OutgoingMessageDto
{
    private AcceptedEnergyResultMessageDto(
        ActorNumber receiverNumber,
        Guid processId,
        EventId eventId,
        string businessReason,
        ActorRole receiverRole,
        AcceptedEnergyResultMessageTimeSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            DocumentType.NotifyAggregatedMeasureData,
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            relatedToMessageId)
    {
        Series = series;
    }

    public AcceptedEnergyResultMessageTimeSeries Series { get; }

    public static AcceptedEnergyResultMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Guid processId,
        EventId eventId,
        string gridAreaCode,
        string meteringPointType,
        string? settlementMethod,
        string measureUnitType,
        string resolution,
        string? energySupplierNumber,
        string? balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<AcceptedEnergyResultMessagePoint> points,
        string businessReasonName,
        long calculationResultVersion,
        string? originalTransactionIdReference = null,
        string? settlementVersion = null,
        MessageId? relatedToMessageId = null)
    {
        var series = new AcceptedEnergyResultMessageTimeSeries(
            TransactionId: Guid.NewGuid(),
            gridAreaCode,
            meteringPointType,
            null,
            settlementMethod,
            measureUnitType,
            resolution,
            energySupplierNumber,
            balanceResponsibleNumber,
            period,
            points.Select(p => new AcceptedEnergyResultMessagePoint(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime)).ToList(),
            calculationResultVersion,
            originalTransactionIdReference,
            settlementVersion);
        return new AcceptedEnergyResultMessageDto(
            receiverNumber,
            processId,
            eventId,
            businessReasonName,
            receiverRole,
            series,
            relatedToMessageId);
    }
}

public record AcceptedEnergyResultMessageTimeSeries(
    Guid TransactionId,
    string GridAreaCode,
    string MeteringPointType,
    string? SettlementType, // TODO: To ensure backwards compatibility, will be remove in another PR.
    string? SettlementMethod,
    string MeasureUnitType,
    string Resolution,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    Period Period,
    IReadOnlyCollection<AcceptedEnergyResultMessagePoint> Point,
    long CalculationResultVersion,
    string? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record AcceptedEnergyResultMessagePoint(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);
