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

using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Formats;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;

public abstract class RequestQueriesBase(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;

    // TODO (MWO): Use DatabricksContract instead of string fields, once energy has been converted maybe?
    protected async Task<List<CalculationTypeForGridArea>> GetCalculationTypeForGridAreasAsync(
        string gridAreaCodeColumnName,
        string calculationTypeColumnName,
        CalculationTypeForGridAreasQueryStatementBase calculationTypeForGridAreaQueryStatement,
        BusinessReason queryParametersBusinessReason,
        SettlementVersion? queryParametersSettlementVersion)
    {
        var calculationTypeForGridAreas = await _databricksSqlWarehouseQueryExecutor
            .ExecuteStatementAsync(calculationTypeForGridAreaQueryStatement, Format.JsonArray)
            .Select(d =>
            {
                var databricksSqlRow = new DatabricksSqlRow(d);
                return new CalculationTypeForGridArea(
                    databricksSqlRow[gridAreaCodeColumnName]!,
                    databricksSqlRow[calculationTypeColumnName]!);
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return calculationTypeForGridAreas
            .GroupBy(ctfga => ctfga.GridArea)
            .Select(grouping =>
            {
                if (!IsQueryForLatestCorrection(queryParametersBusinessReason, queryParametersSettlementVersion))
                {
                    return new CalculationTypeForGridArea(
                        grouping.Key,
                        CalculationTypeMapper.ToDeltaTableValue(
                            queryParametersBusinessReason,
                            queryParametersSettlementVersion));
                }

                var calculationTypes = grouping.Select(ctfga => ctfga.CalculationType).ToList();

                if (calculationTypes.Contains(DeltaTableCalculationType.ThirdCorrectionSettlement))
                {
                    return new CalculationTypeForGridArea(grouping.Key, DeltaTableCalculationType.ThirdCorrectionSettlement);
                }

                if (calculationTypes.Contains(DeltaTableCalculationType.SecondCorrectionSettlement))
                {
                    return new CalculationTypeForGridArea(grouping.Key, DeltaTableCalculationType.SecondCorrectionSettlement);
                }

                if (calculationTypes.Contains(DeltaTableCalculationType.FirstCorrectionSettlement))
                {
                    return new CalculationTypeForGridArea(grouping.Key, DeltaTableCalculationType.FirstCorrectionSettlement);
                }

                return new CalculationTypeForGridArea(grouping.Key, DeltaTableCalculationType.WholesaleFixing);
            })
            .Where(ctfga => ctfga.CalculationType != DeltaTableCalculationType.WholesaleFixing)
            .Distinct()
            .ToList();
    }

    protected async IAsyncEnumerable<TSeries> CreateSeriesPackagesAsync<TSeries, TPoint>(
        Func<DatabricksSqlRow, IReadOnlyCollection<TPoint>, TSeries> createSeries,
        Func<DatabricksSqlRow, DatabricksSqlRow, bool> isNewPackage,
        Func<DatabricksSqlRow, TPoint> createPoint,
        DatabricksStatement sqlStatement)
    {
        var points = new List<TPoint>();
        DatabricksSqlRow? previous = null;

        await foreach (var databricksCurrentRow in _databricksSqlWarehouseQueryExecutor
                           .ExecuteStatementAsync(sqlStatement, Format.JsonArray).ConfigureAwait(false))
        {
            var current = new DatabricksSqlRow(databricksCurrentRow);

            if (previous != null && isNewPackage(current, previous))
            {
                yield return createSeries(previous, points);
                points = [];
            }

            points.Add(createPoint(current));
            previous = current;
        }

        if (previous != null)
        {
            yield return createSeries(previous, points);
        }
    }

    private static bool IsQueryForLatestCorrection(BusinessReason businessReason, SettlementVersion? settlementVersion)
    {
        return businessReason == BusinessReason.Correction
               && settlementVersion is null;
    }
}
