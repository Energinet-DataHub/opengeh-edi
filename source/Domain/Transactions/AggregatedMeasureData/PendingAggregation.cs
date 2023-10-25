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

//using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
// using AGridAreas = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.GridAreaDetails;
// using APeriod = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Period;
// using APoint = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Point;

using NodaTime;

namespace Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;

public class PendingAggregation
{
    public PendingAggregation(
        IReadOnlyList<Point> points,
        string meteringPointType,
        string measureUnitType,
        string resolution,
        string? settlementType,
        string businessReason,
        ProcessId processId,
        Instant start,
        Instant end,
        string gridAreaCode,
        string gridAreaResponsibleId,
        string? settlementVersion = null,
        string? receiverRole = null,
        string? receiver = null,
        string? originalTransactionIdReference = null,
        string? energySupplierId = null,
        string? balanceResponsibleId = null)
    {
        Points = points;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
        SettlementType = settlementType;
        BusinessReason = businessReason;
        ProcessId = processId;
        SettlementVersion = settlementVersion;
        ReceiverRole = receiverRole;
        Receiver = receiver;
        OriginalTransactionIdReference = originalTransactionIdReference;
        Start = start;
        End = end;
        EnergySupplierId = energySupplierId;
        BalanceResponsibleId = balanceResponsibleId;
        GridAreaCode = gridAreaCode;
        GridAreaResponsibleId = gridAreaResponsibleId;
    }

    // private PendingAggregation(
    //     IReadOnlyList<Point> points,
    //     string meteringPointType,
    //     string measureUnitType,
    //     string resolution,
    //     string start,
    //     string end,
    //     //string? settlementType,
    //     string businessReason,
    //     string? energySupplierId,
    //     string? balanceResponsibleId,
    //     string gridAreaCode,
    //     string gridAreaResponsibleId,
    //     ProcessId processId)
    //     //string? settlementVersion = null,
    //     //string? receiverRole = null,
    //     //string? receiver = null,
    //     //tring? originalTransactionIdReference = null)
    // {
    //     Points = points;
    //     MeteringPointType = meteringPointType;
    //     MeasureUnitType = measureUnitType;
    //     Resolution = resolution;
    //     Period = new Period(InstantPattern.General.Parse(start).Value, InstantPattern.General.Parse(end).Value);
    //     //SettlementType = settlementType;
    //     BusinessReason = businessReason;
    //     ActorGrouping = new ActorGrouping(energySupplierId, balanceResponsibleId);
    //     GridAreaDetails = new GridAreaDetails(gridAreaCode, gridAreaResponsibleId);
    //     ProcessId = processId;
    //     //SettlementVersion = settlementVersion;
    //     //ReceiverRole = receiverRole;
    //     //Receiver = receiver;
    //     //riginalTransactionIdReference = originalTransactionIdReference;
    // }

    private PendingAggregation(
            IReadOnlyList<Point> points,
            string meteringPointType,
            string measureUnitType,
            string resolution,
            Instant start,
            Instant end,
            string businessReason,
            string? energySupplierId,
            string? balanceResponsibleId,
            string gridAreaCode,
            string gridAreaResponsibleId,
            ProcessId processId)
    {
        Points = points;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
        BusinessReason = businessReason;
        EnergySupplierId = energySupplierId;
        BalanceResponsibleId = balanceResponsibleId;
        ProcessId = processId;
        Start = start;
        End = end;
        GridAreaCode = gridAreaCode;
        GridAreaResponsibleId = gridAreaResponsibleId;
        Id = Guid.NewGuid();
    }

// #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
//     private PendingAggregation() { }
// #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public IReadOnlyList<Point> Points { get;  }

    public string MeteringPointType { get;  }

    public string MeasureUnitType { get;  }

    public string Resolution { get;  }

    public Period Period => new Period(Start, End);

    public string? SettlementType { get;  }

    public string BusinessReason { get;  }

    public ActorGrouping ActorGrouping => new ActorGrouping(EnergySupplierId, BalanceResponsibleId);

    public GridAreaDetails GridAreaDetails => new GridAreaDetails(GridAreaCode, GridAreaResponsibleId);

    public ProcessId ProcessId { get;  }

    public string? SettlementVersion { get;  }

    public string? ReceiverRole { get;  }

    public string? Receiver { get;  }

    public string? OriginalTransactionIdReference { get;  }

    private Instant Start { get; }

    private Instant End { get; }

    private string? EnergySupplierId { get; }

    private string? BalanceResponsibleId { get; }

    private string GridAreaCode { get; }

    private string GridAreaResponsibleId { get; }

    private Guid Id { get; }
}

// public record PendingAggregation(
//     IReadOnlyList<Point> Points,
//     string MeteringPointType,
//     string MeasureUnitType,
//     string Resolution,
//     Period Period,
//     string? SettlementType,
//     string BusinessReason,
//     ActorGrouping ActorGrouping,
//     GridAreaDetails GridAreaDetails,
//     ProcessId ProcessId,
//     string? SettlementVersion = null,
//     string? ReceiverRole = null,
//     string? Receiver = null,
//     string? OriginalTransactionIdReference = null);

public record ActorGrouping(string? EnergySupplierNumber, string? BalanceResponsibleNumber);
