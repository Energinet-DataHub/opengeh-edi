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
    public MarketEvaluationPoint(string id, string meteringPointResponsible, string type, string settlementMethod, string meteringMethod, string connectionState, string readCycle, string netSettlementGroup, string nextReadingDate, string meteringGridAreaId, string inMeteringGridAreaId, string outMeteringGridAreaId, string linkedMarketEvaluationPointId, string physicalConnectionCapacity, string connectionType, string disconnectionMethod, string psrType, string productionObligation, string contractedConnectionCapacity, string ratedCurrent, string meterId, ReadOnlyCollection<Series> series, string energySupplier, Instant supplyStart, string description, string geoInfoReference, Address mainAddress, string isActualAddress, ParentMarketEvaluationPoint parentMktEvaluationPoint, ChildMarketEvaluationPoint childMktEvaluationPoint)
    {
        Id = id;
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
        ParentMktEvaluationPoint = parentMktEvaluationPoint;
        ChildMktEvaluationPoint = childMktEvaluationPoint;
    }

    public string Id { get; }

    public string MeteringPointResponsible { get; }

    public string Type { get; }

    public string SettlementMethod { get; }

    public string MeteringMethod { get; }

    public string ConnectionState { get; }

    public string ReadCycle { get; }

    public string NetSettlementGroup { get; }

    public string NextReadingDate { get; }

    public string MeteringGridAreaId { get; }

    public string InMeteringGridAreaId { get; }

    public string OutMeteringGridAreaId { get; }

    public string LinkedMarketEvaluationPointId { get; }

    public string PhysicalConnectionCapacity { get; }

    public string ConnectionType { get; }

    public string DisconnectionMethod { get; }

    public string PsrType { get; }

    public string ProductionObligation { get; }

    public string ContractedConnectionCapacity { get; }

    public string RatedCurrent { get; }

    public string MeterId { get; }

    public ReadOnlyCollection<Series> Series { get; }

    public string EnergySupplier { get; }

    public Instant SupplyStart { get; }

    public string Description { get; }

    public string GeoInfoReference { get; }

    public Address MainAddress { get; }

    public string IsActualAddress { get; }

    public ParentMarketEvaluationPoint ParentMktEvaluationPoint { get; }

    public ChildMarketEvaluationPoint ChildMktEvaluationPoint { get; }
}
