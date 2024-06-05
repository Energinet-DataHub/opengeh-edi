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

using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

public class EnergyResultEnumerator(
    DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
    IOptions<EdiDatabricksOptions> ediDatabricksOptions,
    ILogger<EnergyResultEnumerator> logger)
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    private readonly EdiDatabricksOptions _ediDatabricksOptions = ediDatabricksOptions.Value;
    private readonly ILogger<EnergyResultEnumerator> _logger = logger;

    public EdiDatabricksOptions EdiDatabricksOptions => _ediDatabricksOptions;

    public async IAsyncEnumerable<EnergyResultPerGridArea> GetAsync(EnergyResultQueryBase query)
    {
        DatabricksSqlRow? currentRow = null;
        var resultCount = 0;
        var timeSeriesPoints = new List<EnergyTimeSeriesPoint>();

        var statement = DatabricksStatement
            .FromRawSql(query.BuildSqlQuery())
            .Build();

        await foreach (var nextRow in _databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(statement).ConfigureAwait(false))
        {
            var timeSeriesPoint = EnergyTimeSeriesPointFactory.CreateTimeSeriesPoint(nextRow);

            if (currentRow != null && BelongsToDifferentResults(currentRow, nextRow))
            {
                yield return EnergyResultPerGridAreaFactory.CreateEnergyResult(currentRow!, timeSeriesPoints);
                resultCount++;
                timeSeriesPoints = [];
            }

            timeSeriesPoints.Add(timeSeriesPoint);
            currentRow = nextRow;
        }

        if (currentRow != null)
        {
            yield return EnergyResultPerGridAreaFactory.CreateEnergyResult(currentRow, timeSeriesPoints);
            resultCount++;
        }

        _logger.LogDebug("Fetched {result_count} energy results for calculation {calculation_id}", resultCount, query.CalculationId);
    }

    private static bool BelongsToDifferentResults(DatabricksSqlRow row, DatabricksSqlRow otherRow)
    {
        return !row.ToGuid(EnergyResultColumnNames.ResultId).Equals(otherRow.ToGuid(EnergyResultColumnNames.ResultId));
    }
}
