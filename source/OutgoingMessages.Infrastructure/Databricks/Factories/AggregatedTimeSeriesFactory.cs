﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using EnergyTimeSeriesPoint = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models.EnergyTimeSeriesPoint;
using ResolutionMapper = Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults.ResolutionMapper;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class AggregatedTimeSeriesFactory
{
    public static AggregatedTimeSeries Create(
        IAggregatedTimeSeriesDatabricksContract databricksContract,
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints)
    {
        var gridArea = databricksSqlRow[databricksContract.GetGridAreaCodeColumnName()];
        var resolution = ResolutionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(databricksContract.GetResolutionColumnName()));
        var (periodStart, periodEnd) = PeriodFactory.GetPeriod(timeSeriesPoints, resolution);
        var (businessReason, settlementVersion) = BusinessReasonAndSettlementVersionMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(databricksContract.GetCalculationTypeColumnName()));
        var meteringPointType = MeteringPointTypeMapper.FromDeltaTableValue(
            databricksSqlRow.ToNonEmptyString(databricksContract.GetMeteringPointTypeColumnName()));
        var settlementMethod = SettlementMethodMapper.FromDeltaTableValue(
            databricksSqlRow[databricksContract.GetSettlementMethodColumnName()]);

        return new AggregatedTimeSeries(
            gridArea: gridArea!,
            timeSeriesPoints: timeSeriesPoints.Select(point => new Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults.EnergyTimeSeriesPoint(
                Time: point.TimeUtc,
                Quantity: point.Quantity,
                Qualities: point.Qualities)).ToArray(),
            meteringPointType: meteringPointType,
            settlementMethod: settlementMethod,
            businessReason: businessReason,
            settlementVersion: settlementVersion,
            periodStart: periodStart,
            periodEnd: periodEnd,
            resolution,
            SqlResultValueConverters.ToInt(databricksSqlRow[databricksContract.GetCalculationVersionColumnName()]!)!.Value);
    }
}
