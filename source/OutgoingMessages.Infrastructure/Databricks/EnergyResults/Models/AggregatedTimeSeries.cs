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
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;

public abstract class AggregatedTimeSeries
{
    public AggregatedTimeSeries(
        string gridAreaCode,
        EnergyTimeSeriesPoint[] timeSeriesPoints,
        MeteringPointType meteringPointType,
        CalculationType calculationType,
        Instant periodStartUtc,
        Instant periodEndUtc,
        Resolution resolution,
        int calculationVersion,
        SettlementMethod? settlementMethod)
    {
        if (timeSeriesPoints.Length == 0)
            throw new ArgumentException($"{nameof(timeSeriesPoints)} are empty.");

        GridAreaCode = gridAreaCode;
        TimeSeriesPoints = timeSeriesPoints;
        MeteringPointType = meteringPointType;
        CalculationType = calculationType;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        Resolution = resolution;
        CalculationVersion = calculationVersion;
        SettlementMethod = settlementMethod;
    }

    public string GridAreaCode { get; init; }

    // TODO: Can we use a read only collection type?
    public EnergyTimeSeriesPoint[] TimeSeriesPoints { get; init; }

    public MeteringPointType MeteringPointType { get; init; }

    public CalculationType CalculationType { get; init; }

    public Instant PeriodStartUtc { get; init; }

    public Instant PeriodEndUtc { get; init; }

    public Resolution Resolution { get; }

    public int CalculationVersion { get; init; }

    public SettlementMethod? SettlementMethod { get; init; }
}
