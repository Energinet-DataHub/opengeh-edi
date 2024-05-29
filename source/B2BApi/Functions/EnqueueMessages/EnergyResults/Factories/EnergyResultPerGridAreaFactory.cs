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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Infrastructure.SqlStatements;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Infrastructure.SqlStatements.Mappers;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.CalculationResults.Interfaces.Model.EnergyResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.Model;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.Factories;

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

        var periodStart = databricksSqlRow[EnergyResultColumnNames.CalculationPeriodStart];
        ArgumentException.ThrowIfNullOrWhiteSpace(periodStart);

        var periodEnd = databricksSqlRow[EnergyResultColumnNames.CalculationPeriodEnd];
        ArgumentException.ThrowIfNullOrWhiteSpace(periodEnd);

        var version = databricksSqlRow[EnergyResultColumnNames.CalculationVersion];
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

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
            SqlResultValueConverters.ToInstant(periodStart),
            SqlResultValueConverters.ToInstant(periodEnd),
            ResolutionMapper.FromDeltaTableValue(resolution),
            SqlResultValueConverters.ToInt(version),
            SettlementMethodMapper.FromDeltaTableValue(settlementMethod));
    }
}
