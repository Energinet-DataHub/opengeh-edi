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

public class EnergyResultMessageDto : OutgoingMessageDto
{
    private EnergyResultMessageDto(
        ActorNumber receiverNumber,
        string eventId,
        string businessReason,
        ActorRole receiverRole,
        EnergyResultMessageTimeSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            DocumentType.NotifyAggregatedMeasureData,
            receiverNumber,
            null,
            eventId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            relatedToMessageId)
    {
        Series = series;
    }

    public EnergyResultMessageTimeSeries Series { get; }

    public static EnergyResultMessageDto Create(
        string eventId,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        string gridAreaCode,
        string meteringPointType,
        string? settlementMethod,
        string measureUnitType,
        string resolution,
        string? energySupplierNumber,
        string? balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<EnergyResultMessagePoint> points,
        string businessReasonName,
        long calculationResultVersion,
        string? settlementVersion = null)
    {
        var series = new EnergyResultMessageTimeSeries(
            Guid.NewGuid(),
            gridAreaCode,
            meteringPointType,
            null,
            settlementMethod,
            measureUnitType,
            resolution,
            energySupplierNumber,
            balanceResponsibleNumber,
            period,
            points.Select(p => new EnergyResultMessagePoint(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime)).ToList(),
            calculationResultVersion,
            null,
            settlementVersion);
        return new EnergyResultMessageDto(
            receiverNumber,
            eventId,
            businessReasonName,
            receiverRole,
            series);
    }
}

public record EnergyResultMessageTimeSeries(
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
    IReadOnlyCollection<EnergyResultMessagePoint> Point,
    long CalculationResultVersion,
    string? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record EnergyResultMessagePoint(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);
