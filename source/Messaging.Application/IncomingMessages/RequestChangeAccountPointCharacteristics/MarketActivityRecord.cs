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

namespace Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;

public class MarketActivityRecord : IMarketActivityRecord
{
    public string Id { get; init; } = string.Empty;

    public string EffectiveDate { get; init; } = string.Empty;

    public MarketEvaluationPoint MarketEvaluationPoint { get; init; } = new();
}

public class MarketEvaluationPoint
{
    public string GsrnNumber { get; init; } = string.Empty;

    public string TypeOfMeteringPoint { get; init; } = string.Empty;

    public string SettlementMethod { get; init; } = string.Empty;

    public string MeteringMethod { get; init; } = string.Empty;

    public string PhysicalStatusOfMeteringPoint { get; init; } = string.Empty;

    public string MeterReadingOccurence { get; init; } = string.Empty;

    public string NetSettlementGroup { get; init; } = string.Empty;

    public string ScheduledMeterReadingDate { get; init; } = string.Empty;

    public string MeteringGridArea { get; init; } = string.Empty;

    public string InMeteringGridArea { get; init; } = string.Empty;

    public string OutMeteringGridArea { get; init; } = string.Empty;

    public string PowerPlant { get; init; } = string.Empty;

    public string PhysicalConnectionCapacity { get; init; } = string.Empty;

    public string ConnectionType { get; init; } = string.Empty;

    public string DisconnectionType { get; init; } = string.Empty;

    public string AssetType { get; init; } = string.Empty;

    public string ProductionObligation { get; init; } = string.Empty;

    public string MaximumPower { get; init; } = string.Empty;

    public string MaximumCurrent { get; init; } = string.Empty;

    public string MeterNumber { get; init; } = string.Empty;

    public Series Series { get; init; } = new(string.Empty, string.Empty);
}

public record Series(string ProductType, string MeasureUnitType);
