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
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;

public class AggregatedTimeSeries
{
    public AggregatedTimeSeries(
        string gridArea,
        EnergyTimeSeriesPoint[] timeSeriesPoints,
        TimeSeriesType timeSeriesType,
        BusinessReason businessReason,
        SettlementVersion? settlementVersion,
        Instant periodStart,
        Instant periodEnd,
        Resolution resolution,
        long version)
    {
        if (timeSeriesPoints.Length == 0)
            throw new ArgumentException($"{nameof(timeSeriesPoints)} are empty.");

        GridArea = gridArea;
        TimeSeriesPoints = timeSeriesPoints;
        TimeSeriesType = timeSeriesType;
        BusinessReason = businessReason;
        SettlementVersion = settlementVersion;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Resolution = resolution;
        Version = version;
    }

    public string GridArea { get; init; }

    /// <summary>
    /// Time series points for the period excluding the end time
    /// </summary>
    public EnergyTimeSeriesPoint[] TimeSeriesPoints { get; init; }

    public TimeSeriesType TimeSeriesType { get; init; }

    public BusinessReason BusinessReason { get; }

    public SettlementVersion? SettlementVersion { get; }

    public Instant PeriodStart { get; init; }

    /// <summary>
    /// The point are exclusive the end time.
    /// </summary>
    public Instant PeriodEnd { get; init; }

    public Resolution Resolution { get; }

    public long Version { get; init; }
}
