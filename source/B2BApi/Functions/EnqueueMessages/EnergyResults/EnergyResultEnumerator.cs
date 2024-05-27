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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.EnergyResults;

public class EnergyResultEnumerator
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<EnergyResultEnumerator> _logger;

    public EnergyResultEnumerator(
        DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
        ILogger<EnergyResultEnumerator> logger)
    {
        _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
        _logger = logger;
    }

    public async IAsyncEnumerable<EnergyResultMessageDto> GetAsync(Guid calculationId)
    {
        var query = new EnergyResultViewQuery(calculationId);
        await foreach (var messageDto in GetInternalAsync(query))
            yield return messageDto;
        _logger.LogDebug("Fetched all energy results for calculation {calculation_id}", calculationId);
    }

    private static bool BelongsToDifferentResults(DatabricksSqlRow row, DatabricksSqlRow otherRow)
    {
        return !row[EnergyResultViewColumnNames.ResultId]!.Equals(otherRow[EnergyResultViewColumnNames.ResultId]);
    }

    // TODO: Here we would like to create/map "EnergyResultMessageDto" to skip one layer of mapping
    private async IAsyncEnumerable<EnergyResultMessageDto> GetInternalAsync(EnergyResultViewQuery query)
    {
        DatabricksSqlRow? currentRow = null;
        var resultCount = 0;
        var timeSeriesPoints = new List<EnergyResultMessagePoint>();


        // TODO: Maybe it would be faster to move to the "new model" if we first convert to "EnergyResultProducedV2" event
        // and then use the factory "EnergyResultMessageResultFactory" to create "EnergyResultMessageDto" ????


        // TODO: Parse/map data from "Energy Result data object" into "Outgoing message" type and send to processor - see "EnergyResultProducedV2Processor"
        ////var message = await _energyResultMessageResultFactory
        ////    .CreateAsync(EventId.From(integrationEvent.EventIdentification), energyResultProducedV2, CancellationToken.None);

        await foreach (var nextRowAsDynamic in _databricksSqlWarehouseQueryExecutor.ExecuteStatementAsync(query, Format.JsonArray).ConfigureAwait(false))
        {
            var nextRow = new DatabricksSqlRow(nextRowAsDynamic);
            var timeSeriesPoint = CreateTimeSeriesPoint(nextRow);

            if (currentRow != null && BelongsToDifferentResults(currentRow, nextRow))
            {
                yield return CreateEnergyResult(currentRow!, timeSeriesPoints);
                resultCount++;
                timeSeriesPoints = [];
            }

            timeSeriesPoints.Add(timeSeriesPoint);
            currentRow = nextRow;
        }

        if (currentRow != null)
        {
            yield return CreateEnergyResult(currentRow, timeSeriesPoints);
            resultCount++;
        }

        _logger.LogDebug("Fetched {result_count} energy results", resultCount);
    }

    private EnergyResultMessageDto CreateEnergyResult(DatabricksSqlRow databricksSqlRow, List<EnergyResultMessagePoint> timeSeriesPoints)
    {
        throw new NotImplementedException();
    }

    private EnergyResultMessagePoint CreateTimeSeriesPoint(DatabricksSqlRow row)
    {
        throw new NotImplementedException();
    }
}
