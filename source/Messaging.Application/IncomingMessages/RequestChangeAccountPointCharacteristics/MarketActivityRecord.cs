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

namespace Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;
public record MarketActivityRecord(
    string Id,
    string EffectiveDate,
    MarketEvaluationPoint MarketEvaluationPoint) : IMarketActivityRecord;

public record MarketEvaluationPoint(
    string GsrnNumber,
    string TypeOfMeteringPoint,
    string SettlementMethod,
    string MeteringMethod,
    string PhysicalStatusOfMeteringPoint,
    string MeterReadingOccurence,
    string NetSettlementGroup,
    string ScheduledMeterReadingDate,
    string MeteringGridArea,
    string InMeteringGridArea,
    string OutMeteringGridArea,
    string PowerPlant,
    string PhysicalConnectionCapacity,
    string ConnectionType,
    string DisconnectionType,
    string AssetType,
    string ProductionObligation,
    string MaximumPower,
    string MaximumCurrent,
    string MeterNumber,
    Series Series,
    string LocationDescription,
    string GeoInfoReference,
    Address Address);

public record Series(string ProductType, string MeasureUnitType);

public record Address(
    string StreetCode,
    string StreetName,
    string BuildingNumber,
    string FloorIdentification,
    string RoomIdentification);
