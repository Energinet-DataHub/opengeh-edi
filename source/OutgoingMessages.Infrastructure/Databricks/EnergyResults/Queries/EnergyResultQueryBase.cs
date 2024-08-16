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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

public abstract class EnergyResultQueryBase<TResult>(
        ILogger logger,
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId,
        DateTimeZone dateTimeZone)
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

    public Guid CalculationId { get; } = calculationId;

    /// <summary>
    /// The schema definition of the view expressed as (Column name, Data type, Is nullable).
    ///
    /// Can be used in tests to create a matching data object (e.g. table).
    /// </summary>
    public abstract Dictionary<string, (string DataType, bool IsNullable)> SchemaDefinition { get; }

    internal DateTimeZone DateTimeZone { get; } = dateTimeZone;

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
            if (previousResult == null || BelongsToSameResultSet(previousResult, currentResult))
            {
                previousResult = currentResult;
                currentResultSet.Add(currentResult);
                continue;
            }

            yield return await CreateResultAsync(currentResultSet).ConfigureAwait(false);

            // Next result
            currentResultSet = [];
            previousResult = currentResult;
            currentResultSet.Add(currentResult);
        }

        // Last result (if any)
        if (currentResultSet.Count != 0)
        {
            yield return await CreateResultAsync(currentResultSet).ConfigureAwait(false);
        }
    }

    protected abstract Task<TResult> CreateEnergyResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints);

    private static EnergyTimeSeriesPoint CreateTimeSeriesPoint(DatabricksSqlRow databricksSqlRow)
    {
        return new EnergyTimeSeriesPoint(
            databricksSqlRow.ToInstant(EnergyResultColumnNames.Time),
            databricksSqlRow.ToDecimal(EnergyResultColumnNames.Quantity),
            QuantityQualitiesMapper.FromDeltaTableValue(databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.QuantityQualities)));
    }

    private async Task<QueryResult<TResult>> CreateResultAsync(IReadOnlyCollection<DatabricksSqlRow> resultRows)
    {
        var firstRow = resultRows.First();
        var resultId = firstRow.ToGuid(EnergyResultColumnNames.ResultId);

        try
        {
            var timeSeriesPoints = new List<EnergyTimeSeriesPoint>();

            foreach (var row in resultRows)
            {
                var timeSeriesPoint = CreateTimeSeriesPoint(row);
                timeSeriesPoints.Add(timeSeriesPoint);
            }

            var result = await CreateEnergyResultAsync(firstRow, timeSeriesPoints).ConfigureAwait(false);
            return QueryResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creating energy result failed for CalculationId='{CalculationId}', ResultId='{ResultId}'.", CalculationId, resultId);
        }

        return QueryResult<TResult>.Error();
    }

    private bool BelongsToSameResultSet(DatabricksSqlRow? previousResult, DatabricksSqlRow currentResult)
    {
        return
            previousResult?.ToGuid(EnergyResultColumnNames.ResultId) == currentResult.ToGuid(EnergyResultColumnNames.ResultId)
            && IsAFollowingResult(previousResult, currentResult);
    }

    private bool IsAFollowingResult(DatabricksSqlRow previousResult, DatabricksSqlRow currentResult)
    {
        var resolution =
            ResolutionMapper.FromDeltaTableValue(
                previousResult.ToNonEmptyString(EnergyResultColumnNames.Resolution));
        var previousTime = previousResult.ToInstant(EnergyResultColumnNames.Time);
        var currentExceptedTime = PeriodFactory.GetEndDateWithResolutionOffset(resolution, previousTime, DateTimeZone);
        return currentExceptedTime == currentResult.ToInstant(EnergyResultColumnNames.Time);
    }

    private string BuildSqlQuery()
    {
        var columnNames = SchemaDefinition.Keys.ToArray();

        return $"""
            SELECT {string.Join(", ", columnNames)}
            FROM {DatabaseName}.{DataObjectName}
            WHERE {EnergyResultColumnNames.CalculationId} = '{CalculationId}'
            ORDER BY {EnergyResultColumnNames.ResultId}, {EnergyResultColumnNames.Time}
            """;
    }
}
