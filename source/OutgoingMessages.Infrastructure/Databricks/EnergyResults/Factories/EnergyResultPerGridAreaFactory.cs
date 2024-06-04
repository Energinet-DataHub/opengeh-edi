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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;

public class EnergyResultPerGridAreaFactory
{
    public static EnergyResultPerGridArea CreateEnergyResult(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyList<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        var typedResolution = ResolutionMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.Resolution));

        var period = GetPeriod(timeSeriesPoints, typedResolution);

        return new EnergyResultPerGridArea(
            databricksSqlRow.ToGuid(EnergyResultColumnNames.ResultId),
            databricksSqlRow.ToGuid(EnergyResultColumnNames.CalculationId),
            databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.GridAreaCode),
            MeteringPointTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.MeteringPointType)),
            timeSeriesPoints.ToArray(),
            CalculationTypeMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.CalculationType)),
            period.Start,
            period.End,
            typedResolution,
            databricksSqlRow.ToLong(EnergyResultColumnNames.CalculationVersion),
            SettlementMethodMapper.FromDeltaTableValue(databricksSqlRow.ToNullableString(EnergyResultColumnNames.SettlementMethod)));
    }

    private static (Instant Start, Instant End) GetPeriod(IReadOnlyList<EnergyTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.TimeUtc);
        var resolutionInMinutes = GetResolutionInMinutes(resolution);
        // The end data is the start of the next period.
        var end = timeSeriesPoints.Max(x => x.TimeUtc).Plus(Duration.FromMinutes(resolutionInMinutes));
        return (start, end);
    }

    private static int GetResolutionInMinutes(Resolution resolution)
    {
        var resolutionInMinutes = 0;
        switch (resolution)
        {
            case var res when res == Resolution.QuarterHourly:
                resolutionInMinutes = 15;
                break;
            case var res when res == Resolution.Hourly:
                resolutionInMinutes = 60;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown databricks resolution");
        }

        return resolutionInMinutes;
    }
}
