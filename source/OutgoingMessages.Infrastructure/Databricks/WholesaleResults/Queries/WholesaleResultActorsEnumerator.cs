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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleResultActorsEnumerator(
    DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
    IOptions<EdiDatabricksOptions> ediDatabricksOptions,
    ILogger<WholesaleResultActorsEnumerator> logger)
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<WholesaleResultActorsEnumerator> _logger = logger;

    public EdiDatabricksOptions EdiDatabricksOptions { get; } = ediDatabricksOptions.Value;

    public async IAsyncEnumerable<string> GetActorsAsync<TResult>(WholesaleResultQueryBase<TResult> query)
        where TResult : OutgoingMessageDto
    {
        var resultCount = 0;

        await foreach (var wholesaleResult in query.GetActorsAsync(_databricksSqlWarehouseQueryExecutor).ConfigureAwait(false))
        {
            yield return wholesaleResult;
            resultCount++;
        }

        _logger.LogDebug("Fetched {result_count} wholesale result actors for calculation {calculation_id}", resultCount, query.CalculationId);
    }
}
