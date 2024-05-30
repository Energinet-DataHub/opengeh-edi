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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;

public class EnergyResultPerGridAreaFactory
{
    public static EnergyResultPerGridArea CreateEnergyResult(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyList<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        var calculationId = databricksSqlRow[EnergyResultColumnNames.CalculationId];
        ArgumentException.ThrowIfNullOrWhiteSpace(calculationId);

        var calculationType = databricksSqlRow[EnergyResultColumnNames.CalculationType];
        ArgumentException.ThrowIfNullOrWhiteSpace(calculationType);

        var periodStartUtc = databricksSqlRow[EnergyResultColumnNames.CalculationPeriodStart];
        ArgumentException.ThrowIfNullOrWhiteSpace(periodStartUtc);

        var periodEndUtc = databricksSqlRow[EnergyResultColumnNames.CalculationPeriodEnd];
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEndUtc);

        var calculationVersion = databricksSqlRow[EnergyResultColumnNames.CalculationVersion];
        ArgumentException.ThrowIfNullOrWhiteSpace(calculationVersion);

        var resultId = databricksSqlRow[EnergyResultColumnNames.ResultId];
        ArgumentException.ThrowIfNullOrWhiteSpace(resultId);

        var gridAreaCode = databricksSqlRow[EnergyResultColumnNames.GridAreaCode];
        ArgumentException.ThrowIfNullOrWhiteSpace(gridAreaCode);

        var meteringPointType = databricksSqlRow[EnergyResultColumnNames.MeteringPointType];
        ArgumentException.ThrowIfNullOrWhiteSpace(meteringPointType);

        var resolution = databricksSqlRow[EnergyResultColumnNames.Resolution];
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);

        var settlementMethod = databricksSqlRow[EnergyResultColumnNames.SettlementMethod];

        return new EnergyResultPerGridArea(
            SqlResultValueConverters.ToGuid(resultId),
            SqlResultValueConverters.ToGuid(calculationId),
            gridAreaCode,
            MeteringPointTypeMapper.FromDeltaTableValue(meteringPointType),
            timeSeriesPoints.ToArray(),
            CalculationTypeMapper.FromDeltaTableValue(calculationType),
            SqlResultValueConverters.ToInstant(periodStartUtc),
            SqlResultValueConverters.ToInstant(periodEndUtc),
            ResolutionMapper.FromDeltaTableValue(resolution),
            SqlResultValueConverters.ToInt(calculationVersion),
            SettlementMethodMapper.FromDeltaTableValue(settlementMethod));
    }
}
