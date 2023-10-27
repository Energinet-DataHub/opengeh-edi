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

using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.GridAreas;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;

namespace Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;

public class PendingAggregation
{
    public PendingAggregation(
        IReadOnlyList<Point> points,
        MeteringPointType meteringPointType,
        MeasurementUnit measurementUnit,
        string resolution,
        SettlementType? settlementType,
        BusinessReason businessReason,
        ProcessId processId,
        Period period,
        GridAreaDetails gridAreaDetails,
        ActorNumber? energySupplierId,
        ActorNumber? balanceResponsibleId,
        SettlementVersion? settlementVersion = null,
        MarketRole? receiverRole = null,
        ActorNumber? receiverId = null,
        BusinessTransactionId? businessTransactionId = null)
    {
        Points = points;
        MeteringPointType = meteringPointType;
        MeasurementUnit = measurementUnit;
        Resolution = resolution;
        SettlementType = settlementType;
        BusinessReason = businessReason;
        ProcessId = processId;
        Period = period;
        GridAreaDetails = gridAreaDetails;
        EnergySupplierId = energySupplierId;
        BalanceResponsibleId = balanceResponsibleId;
        SettlementVersion = settlementVersion;
        ReceiverRole = receiverRole;
        ReceiverId = receiverId;
        BusinessTransactionId = businessTransactionId;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private PendingAggregation() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IReadOnlyList<Point> Points { get;  }

    public MeteringPointType MeteringPointType { get;  }

    public MeasurementUnit MeasurementUnit { get;  }

    public string Resolution { get;  }

    public Period Period { get; }

    public SettlementType? SettlementType { get;  }

    public BusinessReason BusinessReason { get;  }

    public ProcessId ProcessId { get;  }

    public GridAreaDetails GridAreaDetails { get; }

    public ActorNumber? BalanceResponsibleId { get; set; }

    public ActorNumber? EnergySupplierId { get; set; }

    public SettlementVersion? SettlementVersion { get;  }

    public MarketRole? ReceiverRole { get;  }

    public ActorNumber? ReceiverId { get;  }

    public BusinessTransactionId? BusinessTransactionId { get;  }

    private Guid Id { get; }
}
