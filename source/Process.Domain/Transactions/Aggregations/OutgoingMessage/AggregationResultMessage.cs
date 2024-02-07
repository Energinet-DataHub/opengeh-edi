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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;

public class AggregationResultMessage : OutgoingMessageDto
{
    private AggregationResultMessage(
        ActorNumber receiverId,
        Guid processId,
        string businessReason,
        ActorRole receiverRole,
        IReadOnlyCollection<TimeSeries> series)
        : base(
            DocumentType.NotifyAggregatedMeasureData,
            receiverId,
            processId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            new Serializer().Serialize(series))
    {
        Series = series;
    }

    public IReadOnlyCollection<TimeSeries> Series { get; }

    public static AggregationResultMessage Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Guid processId,
        GridAreaDetails gridAreaDetails,
        string meteringPointType,
        string? settlementType,
        string measureUnitType,
        string resolution,
        string? energySupplierNumber,
        string? balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<Point> points,
        string businessReasonName,
        long calculationResultVersion,
        string? originalTransactionIdReference = null,
        string? settlementVersion = null)
    {
        ArgumentNullException.ThrowIfNull(gridAreaDetails);

        var series = new TimeSeries(
            processId,
            gridAreaDetails.GridAreaCode,
            meteringPointType,
            settlementType,
            measureUnitType,
            resolution,
            energySupplierNumber,
            balanceResponsibleNumber,
            period,
            points.Select(p => new Point(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime)).ToList(),
            calculationResultVersion,
            originalTransactionIdReference,
            settlementVersion);
        return new AggregationResultMessage(
            receiverNumber,
            processId,
            businessReasonName,
            receiverRole,
            new List<TimeSeries>() { series });
    }

    public static AggregationResultMessage Create(
        ProcessId processId,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        BusinessReason businessReason,
        BusinessTransactionId transactionIdReference,
        SettlementType? settlementType,
        ActorNumber? energySupplierNumber,
        ActorNumber? balanceResponsibleNumber,
        SettlementVersion? settlementVersion,
        IReadOnlyCollection<AggregatedTimeSerie> series)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentNullException.ThrowIfNull(transactionIdReference);
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);

        var timeSeries = new List<TimeSeries>();
        foreach (var aggregatedTimeSeries in series)
        {
            timeSeries.Add(new TimeSeries(
                processId.Id,
                aggregatedTimeSeries.GridAreaDetails.GridAreaCode,
                aggregatedTimeSeries.MeteringPointType,
                settlementType?.Code,
                aggregatedTimeSeries.UnitType,
                aggregatedTimeSeries.Resolution,
                energySupplierNumber?.Value,
                balanceResponsibleNumber?.Value,
                new Period(aggregatedTimeSeries.StartOfPeriod, aggregatedTimeSeries.EndOfPeriod),
                aggregatedTimeSeries.Points.Select(p => new Point(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime)).ToList(),
                aggregatedTimeSeries.CalculationResultVersion,
                transactionIdReference.Id,
                settlementVersion?.Code));
        }

        return new AggregationResultMessage(
            receiverNumber,
            processId.Id,
            businessReason.Name,
            receiverRole,
            timeSeries.ToList());
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
    IReadOnlyCollection<Point> Point,
    long CalculationResultVersion,
    string? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record Point(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);
