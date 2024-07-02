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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public abstract class WholesaleResultQueryBase<TResult>(
        ILogger logger,
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId)
    : IDeltaTableSchemaDescription
    where TResult : OutgoingMessageDto
{
    private readonly ILogger _logger = logger;
    private readonly EdiDatabricksOptions _ediDatabricksOptions = ediDatabricksOptions;

    /// <summary>
    /// Name of database to query in.
    /// </summary>
    public string DatabaseName => _ediDatabricksOptions.DatabaseName;

    /// <summary>
    /// Name of view or table to query in.
    /// </summary>
    public abstract string DataObjectName { get; }

    /// <summary>
    /// Name of view or table column that holds the actor for the query.
    /// </summary>
    public abstract string ActorColumnName { get; }

    public Guid CalculationId { get; } = calculationId;

    /// <summary>
    /// The schema definition of the view expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public abstract Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }

    internal async IAsyncEnumerable<QueryResult<TResult>> GetAsync(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        ArgumentNullException.ThrowIfNull(databricksSqlWarehouseQueryExecutor);

        var statement = DatabricksStatement
            .FromRawSql(BuildSqlQuery())
            .Build();

        Guid? currentResultId = null;
        var currentResultRows = new List<DatabricksSqlRow>();

        await foreach (var row in databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(statement).ConfigureAwait(false))
        {
            var rowResultId = row.ToGuid(WholesaleResultColumnNames.ResultId);

            if (IsFirstRow(currentResultId))
            {
                currentResultId = rowResultId;
            }

            if (IsSameResult(currentResultId, rowResultId))
            {
                currentResultRows.Add(row);
                continue;
            }

            yield return await CreateResultAsync(currentResultRows).ConfigureAwait(false);

            // Next result
            currentResultRows = [];
            currentResultId = rowResultId;
            currentResultRows.Add(row);
        }

        // Last result (if any)
        if (currentResultRows.Count != 0)
        {
            yield return await CreateResultAsync(currentResultRows).ConfigureAwait(false);
        }
    }

    internal async IAsyncEnumerable<string> GetActorsAsync(DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor)
    {
        ArgumentNullException.ThrowIfNull(databricksSqlWarehouseQueryExecutor);

        var selectActorsStatement = DatabricksStatement
            .FromRawSql(BuildActorsSqlQuery())
            .Build();

        await foreach (var row in databricksSqlWarehouseQueryExecutor.ExecuteQueryAsync(selectActorsStatement).ConfigureAwait(false))
        {
            var actorNumber = row.ToNonEmptyString(ActorColumnName);

            yield return actorNumber;
        }
    }

    protected abstract Task<TResult> CreateWholesaleResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints);

    private static WholesaleTimeSeriesPoint CreateTimeSeriesPoint(DatabricksSqlRow databricksSqlRow)
    {
        return new WholesaleTimeSeriesPoint(
            databricksSqlRow.ToInstant(WholesaleResultColumnNames.Time),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Quantity),
            QuantityQualitiesMapper.TryFromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityQualities)),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Price),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Amount));
    }

    private static bool IsFirstRow(Guid? currentResultId)
    {
        return currentResultId == null;
    }

    private static bool IsSameResult(Guid? currentResultId, Guid rowResultId)
    {
        return currentResultId == rowResultId;
    }

    private async Task<QueryResult<TResult>> CreateResultAsync(IReadOnlyCollection<DatabricksSqlRow> resultRows)
    {
        var firstRow = resultRows.First();
        var resultId = firstRow.ToGuid(WholesaleResultColumnNames.ResultId);

        try
        {
            var timeSeriesPoints = new List<WholesaleTimeSeriesPoint>();

            foreach (var row in resultRows)
            {
                var timeSeriesPoint = CreateTimeSeriesPoint(row);
                timeSeriesPoints.Add(timeSeriesPoint);
            }

            var result = await CreateWholesaleResultAsync(firstRow, timeSeriesPoints).ConfigureAwait(false);
            return QueryResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creating energy result failed for CalculationId='{CalculationId}', ResultId='{ResultId}'.", CalculationId, resultId);
        }

        return QueryResult<TResult>.Error();
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

    private string BuildActorsSqlQuery()
    {
        return $"""
                SELECT DISTINCT {ActorColumnName}
                FROM {DatabaseName}.{DataObjectName}
                WHERE {WholesaleResultColumnNames.CalculationId} = '{CalculationId}'
                """;
    }
}
