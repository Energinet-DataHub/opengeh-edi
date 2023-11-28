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
using Energinet.DataHub.EDI.BuildingBlocks.Domain;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;

public class AggregationResultMessage : OutgoingMessageDto
{
    // private AggregationResultMessage(ActorNumber receiverId, Guid processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
    //     : base(DocumentType.NotifyAggregatedMeasureData, receiverId, processId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    // {
    //     Series = new Serializer().Deserialize<TimeSeries>(messageRecord)!;
    // }

    private AggregationResultMessage(ActorNumber receiverId, Guid processId, string businessReason, MarketRole receiverRole, TimeSeries series)
        : base(DocumentType.NotifyAggregatedMeasureData, receiverId, processId, businessReason, receiverRole, DataHubDetails.IdentificationNumber, MarketRole.MeteringDataAdministrator, new Serializer().Serialize(series))
    {
        Series = series;
    }

    public TimeSeries Series { get; }

    public static AggregationResultMessage Create(
        ActorNumber receiverNumber,
        MarketRole receiverRole,
        Guid processId,
        string gridAreaCode,
        string meteringPointType,
        string? settlementType,
        string measureUnitType,
        string resolution,
        string? energySupplierNumber,
        string? balanceResponsibleNumber,
        Period period,
        IReadOnlyList<Point> points,
        string businessReasonName,
        string? originalTransactionIdReference = null,
        string? settlementVersion = null)
    {
        var series = new TimeSeries(
            processId,
            gridAreaCode,
            meteringPointType,
            settlementType,
            measureUnitType,
            resolution,
            energySupplierNumber,
            balanceResponsibleNumber,
            period,
            points.Select(p => new Point(p.Position, p.Quantity, p.Quality, p.SampleTime)).ToList(),
            originalTransactionIdReference,
            settlementVersion);
        return new AggregationResultMessage(
            receiverNumber,
            processId,
            businessReasonName,
            receiverRole,
            series);
    }
}

public record TimeSeries(
    Guid TransactionId,
    string GridAreaCode,
    string MeteringPointType,
    string? SettlementType,
    string MeasureUnitType,
    string Resolution,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    Period Period,
    IReadOnlyList<Point> Point,
    string? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record Point(int Position, decimal? Quantity, string Quality, string SampleTime);
