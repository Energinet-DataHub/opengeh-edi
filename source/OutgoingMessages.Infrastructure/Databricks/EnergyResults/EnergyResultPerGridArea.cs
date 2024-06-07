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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults;

public sealed class EnergyResultPerGridArea(
    Guid id,
    Guid calculationId,
    string gridAreaCode,
    MeteringPointType meteringPointType,
    EnergyTimeSeriesPoint[] timeSeriesPoints,
    CalculationType calculationType,
    Instant periodStartUtc,
    Instant periodEndUtc,
    Resolution resolution,
    long calculationVersion,
    SettlementMethod? settlementMethod)
    : AggregatedTimeSeries(
        gridAreaCode,
        timeSeriesPoints,
        meteringPointType,
        calculationType,
        periodStartUtc,
        periodEndUtc,
        resolution,
        calculationVersion,
        settlementMethod)
{
    public Guid Id { get; } = id;

    public Guid CalculationId { get; } = calculationId;
}
