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

using System.Collections.Immutable;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public class WholesaleResultEnumerator(
    DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
    IOptions<EdiDatabricksOptions> ediDatabricksOptions,
    ILogger<WholesaleResultEnumerator> logger,
    IMasterDataClient masterDataClient)
{
    private readonly DatabricksSqlWarehouseQueryExecutor _databricksSqlWarehouseQueryExecutor = databricksSqlWarehouseQueryExecutor;
    private readonly ILogger<WholesaleResultEnumerator> _logger = logger;
    private readonly IMasterDataClient _masterDataClient = masterDataClient;

    public EdiDatabricksOptions EdiDatabricksOptions { get; } = ediDatabricksOptions.Value;

    public async IAsyncEnumerable<QueryResult<TResult>> GetAsync<TResult>(WholesaleResultQueryBase<TResult> query)
        where TResult : OutgoingMessageDto
    {
        var builder = ImmutableDictionary.CreateBuilder<string, ActorNumber>();
        foreach (var gridAreaOwner in await _masterDataClient
                     .GetAllGridAreaOwnersAsync(CancellationToken.None)
                     .ConfigureAwait(false))
        {
            builder.Add(gridAreaOwner.GridAreaCode, gridAreaOwner.ActorNumber);
        }

        var gridAreaOwnerDictionary = builder.ToImmutable();

        var resultCount = 0;

        await foreach (var wholesaleResult in query
                           .GetAsync(_databricksSqlWarehouseQueryExecutor, gridAreaOwnerDictionary)
                           .ConfigureAwait(false))
        {
            yield return wholesaleResult;
            resultCount++;
        }

        _logger.LogDebug("Fetched {result_count} wholesale results for calculation {calculation_id}", resultCount, query.CalculationId);
    }
}
