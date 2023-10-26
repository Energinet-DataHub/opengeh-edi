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
        Period period,
        GridAreaDetails gridAreaDetails,
        ActorGrouping actorGrouping,
        string? settlementVersion = null,
        string? receiverRole = null,
        string? receiver = null,
        string? originalTransactionIdReference = null)
    {
        Points = points;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
        SettlementType = settlementType;
        BusinessReason = businessReason;
        ProcessId = processId;
        Period = period;
        GridAreaDetails = gridAreaDetails;
        ActorGrouping = actorGrouping;
        SettlementVersion = settlementVersion;
        ReceiverRole = receiverRole;
        Receiver = receiver;
        OriginalTransactionIdReference = originalTransactionIdReference;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private PendingAggregation() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IReadOnlyList<Point> Points { get;  }

    public string MeteringPointType { get;  }

    public string MeasureUnitType { get;  }

    public string Resolution { get;  }

    public Period Period { get; }

    public string? SettlementType { get;  }

    public string BusinessReason { get;  }

    public ActorGrouping ActorGrouping { get; }

    public GridAreaDetails GridAreaDetails { get; }

    public ProcessId ProcessId { get;  }

    public string? SettlementVersion { get;  }

    public string? ReceiverRole { get;  }

    public string? Receiver { get;  }

    public string? OriginalTransactionIdReference { get;  }

    private Guid Id { get; }
}

public record ActorGrouping(string? EnergySupplierNumber, string? BalanceResponsibleNumber);
