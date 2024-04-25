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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;

[Serializable]
public record AcceptedEnergyResultTimeSeries(
    IReadOnlyCollection<Point> Points,
    MeteringPointType MeteringPointType,
    SettlementMethod? SettlementMethod,
    MeasurementUnit UnitType,
    Resolution Resolution,
    // GridAreaDetails GridAreaDetails, // TODO: What is this used for (operator number)? It seems unused
    string GridAreaCode,
    long CalculationResultVersion,
    Instant StartOfPeriod,
    Instant EndOfPeriod);

[Serializable]
public record Point(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);

[Serializable]
public record GridAreaDetails(string GridAreaCode, string OperatorNumber);
