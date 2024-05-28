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

using NodaTime;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.Model;

public abstract class AggregatedTimeSeries
{
    public AggregatedTimeSeries(
        string gridAreaCode,
        EnergyTimeSeriesPoint[] timeSeriesPoints,
        MeteringPointType meteringPointType,
        CalculationType calculationType,
        Instant periodStart,
        Instant periodEnd,
        Resolution resolution,
        int version,
        SettlementMethod? settlementMethod)
    {
        if (timeSeriesPoints.Length == 0)
            throw new ArgumentException($"{nameof(timeSeriesPoints)} are empty.");

        GridAreaCode = gridAreaCode;
        TimeSeriesPoints = timeSeriesPoints;
        MeteringPointType = meteringPointType;
        CalculationType = calculationType;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        Resolution = resolution;
        Version = version;
        SettlementMethod = settlementMethod;
    }

    public string GridAreaCode { get; init; }

    public EnergyTimeSeriesPoint[] TimeSeriesPoints { get; init; }

    public MeteringPointType MeteringPointType { get; init; }

    public CalculationType CalculationType { get; init; }

    public Instant PeriodStart { get; init; }

    public Instant PeriodEnd { get; init; }

    public Resolution Resolution { get; }

    public int Version { get; init; }

    public SettlementMethod? SettlementMethod { get; init; }
}
