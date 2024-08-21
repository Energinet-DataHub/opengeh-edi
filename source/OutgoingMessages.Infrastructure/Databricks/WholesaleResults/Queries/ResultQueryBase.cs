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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

/// <summary>
/// Common base class for querying calculation results from Databricks.
/// </summary>
public abstract class ResultQueryBase<TResult>(
    EdiDatabricksOptions ediDatabricksOptions,
    Guid calculationId)
    : IDeltaTableSchemaDescription
    where TResult : OutgoingMessageDto
{
    /// <summary>
    /// Name of database to query in.
    /// </summary>
    public string DatabaseName => ediDatabricksOptions.DatabaseName;

    /// <summary>
    /// Name of view or table to query in.
    /// </summary>
    public abstract string DataObjectName { get; }

    /// <summary>
    /// The schema definition of the view expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public abstract Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }

    public Guid CalculationId { get; } = calculationId;

    internal async IAsyncEnumerable<QueryResult<TResult>> GetAsync(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        ArgumentNullException.ThrowIfNull(databricksSqlWarehouseQueryExecutor);

        var statement = DatabricksStatement
            .FromRawSql(BuildSqlQuery())
            .Build();

        DatabricksSqlRow? previousResult = null;
        var currentResultSet = new List<DatabricksSqlRow>();

        await foreach (var currentResult in databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(statement).ConfigureAwait(false))
        {
            if (previousResult == null || BelongsToSameResultSet(currentResult, previousResult))
            {
                currentResultSet.Add(currentResult);
                previousResult = currentResult;
                continue;
            }

            yield return await CreateResultAsync(currentResultSet).ConfigureAwait(false);

            // Next result serie
            currentResultSet =
            [
                currentResult,
            ];
            previousResult = currentResult;
        }

        // Last result (if any)
        if (currentResultSet.Count != 0)
        {
            yield return await CreateResultAsync(currentResultSet).ConfigureAwait(false);
        }
    }

    protected abstract Task<QueryResult<TResult>> CreateResultAsync(List<DatabricksSqlRow> currentResultSet);

    protected abstract bool BelongsToSameResultSet(DatabricksSqlRow currentResult, DatabricksSqlRow previousResult);

    protected abstract string BuildSqlQuery();
}
