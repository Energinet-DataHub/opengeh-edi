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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using NodaTime;

namespace Messaging.Application.OutgoingMessages.AccountingPointCharacteristics;

public class MarketEvaluationPoint
{
    public MarketEvaluationPoint(
        Mrid mrid,
        Mrid meteringPointResponsible,
        string type,
        string settlementMethod,
        string meteringMethod,
        string connectionState,
        string readCycle,
        string netSettlementGroup,
        string nextReadingDate,
        Mrid meteringGridAreaId,
        Mrid inMeteringGridAreaId,
        Mrid outMeteringGridAreaId,
        Mrid linkedMarketEvaluationPointId,
        UnitValue physicalConnectionCapacity,
        string connectionType,
        string disconnectionMethod,
        string psrType,
        string productionObligation,
        UnitValue contractedConnectionCapacity,
        UnitValue ratedCurrent,
        string meterId,
        Series series,
        Mrid energySupplier,
        Instant supplyStart,
        string description,
        string geoInfoReference,
        Address mainAddress,
        string isActualAddress,
        RelatedMarketEvaluationPoint parentMarketEvaluationPoint,
        RelatedMarketEvaluationPoint childMarketEvaluationPoint)
    {
        MRID = mrid;
        MeteringPointResponsible = meteringPointResponsible;
        Type = type;
        SettlementMethod = settlementMethod;
        MeteringMethod = meteringMethod;
        ConnectionState = connectionState;
        ReadCycle = readCycle;
        NetSettlementGroup = netSettlementGroup;
        NextReadingDate = nextReadingDate;
        MeteringGridAreaId = meteringGridAreaId;
        InMeteringGridAreaId = inMeteringGridAreaId;
        OutMeteringGridAreaId = outMeteringGridAreaId;
        LinkedMarketEvaluationPointId = linkedMarketEvaluationPointId;
        PhysicalConnectionCapacity = physicalConnectionCapacity;
        ConnectionType = connectionType;
        DisconnectionMethod = disconnectionMethod;
        PsrType = psrType;
        ProductionObligation = productionObligation;
        ContractedConnectionCapacity = contractedConnectionCapacity;
        RatedCurrent = ratedCurrent;
        MeterId = meterId;
        Series = series;
        EnergySupplier = energySupplier;
        SupplyStart = supplyStart;
        Description = description;
        GeoInfoReference = geoInfoReference;
        MainAddress = mainAddress;
        IsActualAddress = isActualAddress;
        ParentMarketEvaluationPoint = parentMarketEvaluationPoint;
        ChildMarketEvaluationPoint = childMarketEvaluationPoint;
    }

    public Mrid MRID { get; }

    public Mrid MeteringPointResponsible { get; }

    public string Type { get; }

    public string SettlementMethod { get; }

    public string MeteringMethod { get; }

    public string ConnectionState { get; }

    public string ReadCycle { get; }

    public string NetSettlementGroup { get; }

    public string NextReadingDate { get; }

    public Mrid MeteringGridAreaId { get; }

    public Mrid InMeteringGridAreaId { get; }

    public Mrid OutMeteringGridAreaId { get; }

    public Mrid LinkedMarketEvaluationPointId { get; }

    public UnitValue PhysicalConnectionCapacity { get; }

    public string ConnectionType { get; }

    public string DisconnectionMethod { get; }

    public string PsrType { get; }

    public string ProductionObligation { get; }

    public UnitValue ContractedConnectionCapacity { get; }

    public UnitValue RatedCurrent { get; }

    public string MeterId { get; }

    public Series Series { get; }

    public Mrid EnergySupplier { get; }

    public Instant SupplyStart { get; }

    public string Description { get; }

    public string GeoInfoReference { get; }

    public Address MainAddress { get; }

    public string IsActualAddress { get; }

    public RelatedMarketEvaluationPoint ParentMarketEvaluationPoint { get; }

    public RelatedMarketEvaluationPoint ChildMarketEvaluationPoint { get; }
}
