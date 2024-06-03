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
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Formats;
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
    private readonly IOptions<EdiDatabricksOptions> _ediDatabricksOptions = ediDatabricksOptions;
    private readonly ILogger<EnergyResultEnumerator> _logger = logger;

    public async IAsyncEnumerable<EnergyResultPerGridArea> GetAsync(Guid calculationId)
    {
        var query = new EnergyResultPerGridAreaQuery(_ediDatabricksOptions, calculationId);
        await foreach (var messageDto in GetInternalAsync(query))
            yield return messageDto;
        _logger.LogDebug("Fetched all energy results for calculation {calculation_id}", calculationId);
    }

    private static bool BelongsToDifferentResults(DatabricksSqlRow row, DatabricksSqlRow otherRow)
    {
        return !row[EnergyResultColumnNames.ResultId]!.Equals(otherRow[EnergyResultColumnNames.ResultId]);
    }

    private async IAsyncEnumerable<EnergyResultPerGridArea> GetInternalAsync(EnergyResultPerGridAreaQuery query)
    {
        DatabricksSqlRow? currentRow = null;
        var resultCount = 0;
        var timeSeriesPoints = new List<EnergyTimeSeriesPoint>();

        await foreach (var nextRowAsDynamic in _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(query, Format.JsonArray).ConfigureAwait(false))
        {
            var nextRow = new DatabricksSqlRow(nextRowAsDynamic);
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

        _logger.LogDebug("Fetched {result_count} energy results", resultCount);
    }
}
