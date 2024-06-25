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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public abstract class WholesaleResultQueryBase<TResult>(
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId)
    : IDeltaTableSchemaDescription
    where TResult : OutgoingMessageDto
{
    private readonly EdiDatabricksOptions _ediDatabricksOptions = ediDatabricksOptions;

    /// <summary>
    /// Name of database to query in.
    /// </summary>
    public string DatabaseName => _ediDatabricksOptions.DatabaseName;

    /// <summary>
    /// Name of view or table to query in.
    /// </summary>
    public abstract string DataObjectName { get; }

    public Guid CalculationId { get; } = calculationId;

    /// <summary>
    /// The schema definition of the view expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public abstract Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }

    internal async IAsyncEnumerable<TResult> GetAsync(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        ArgumentNullException.ThrowIfNull(databricksSqlWarehouseQueryExecutor);

        DatabricksSqlRow? currentRow = null;
        var timeSeriesPoints = new List<WholesaleTimeSeriesPoint>();

        var statement = DatabricksStatement
            .FromRawSql(BuildSqlQuery())
            .Build();

        var listAsync = await ToListAsync<DatabricksSqlRow>(databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(statement)).ConfigureAwait(false);
        await foreach (var nextRow in databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(statement).ConfigureAwait(false))
        {
            var timeSeriesPoint = WholesaleTimeSeriesPointFactory.Create(nextRow);
            var currentResult = Guid.Empty;
            var nextRowResult = Guid.Empty;
            if (currentRow != null)
            {
                currentResult = currentRow!.ToGuid(WholesaleResultColumnNames.ResultId);
                nextRowResult = nextRow.ToGuid(WholesaleResultColumnNames.ResultId);
            }

            if (currentRow != null && BelongsToDifferentResults(currentRow, nextRow))
            {
                yield return await CreateWholesaleResultAsync(currentRow!, timeSeriesPoints).ConfigureAwait(false);
                timeSeriesPoints = [];
            }

            timeSeriesPoints.Add(timeSeriesPoint);
            currentRow = nextRow;
        }

        if (currentRow != null)
        {
            yield return await CreateWholesaleResultAsync(currentRow, timeSeriesPoints).ConfigureAwait(false);
        }
    }

    protected abstract Task<TResult> CreateWholesaleResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints);

    private static bool BelongsToDifferentResults(DatabricksSqlRow row, DatabricksSqlRow otherRow)
    {
        return !row.ToGuid(WholesaleResultColumnNames.ResultId).Equals(otherRow.ToGuid(WholesaleResultColumnNames.ResultId));
    }

    private static async Task<List<T>> ToListAsync<T>(
        IAsyncEnumerable<T> items,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken)
                           .ConfigureAwait(false))
        {
            results.Add(item);
        }

        return results;
    }

    private string BuildSqlQuery()
    {
        var columnNames = SchemaDefinition.Keys.ToArray();

        return $"""
            SELECT {string.Join(", ", columnNames)}
            FROM {DatabaseName}.{DataObjectName}
            WHERE {WholesaleResultColumnNames.CalculationId} = '{CalculationId}'
            ORDER BY {WholesaleResultColumnNames.ResultId}, {WholesaleResultColumnNames.Time}
            """;
    }
}
