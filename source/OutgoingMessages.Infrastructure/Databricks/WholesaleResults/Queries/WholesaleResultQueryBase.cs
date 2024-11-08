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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;

public abstract class WholesaleResultQueryBase<TResult>(
        ILogger logger,
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId,
        string? energySupplier)
    : CalculationResultQueryBase<TResult>(ediDatabricksOptions, calculationId)
    where TResult : OutgoingMessageDto
{
    private readonly ILogger _logger = logger;
    private readonly string? _energySupplier = energySupplier;

    /// <summary>
    /// Name of view or table column that holds the actor for the query.
    /// </summary>
    protected abstract string ActorColumnName { get; }

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

    protected override async Task<QueryResult<TResult>> CreateResultAsync(
        List<DatabricksSqlRow> currentResultSet,
        ImmutableDictionary<string, ActorNumber>? gridAreaOwnerDictionary)
    {
        var firstRow = currentResultSet.First();
        var resultId = firstRow.ToGuid(WholesaleResultColumnNames.ResultId);

        try
        {
            var timeSeriesPoints = new List<WholesaleTimeSeriesPoint>();

            foreach (var row in currentResultSet)
            {
                var timeSeriesPoint = CreateTimeSeriesPoint(row);
                timeSeriesPoints.Add(timeSeriesPoint);
            }

            var result = await CreateWholesaleResultAsync(firstRow, timeSeriesPoints)
                .ConfigureAwait(false);
            return QueryResult<TResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Creating energy result failed for CalculationId='{CalculationId}', ResultId='{ResultId}'.", CalculationId, resultId);
        }

        return QueryResult<TResult>.Error();
    }

    protected override bool BelongsToSameResultSet(DatabricksSqlRow currentResult, DatabricksSqlRow? previousResult)
    {
        return
            previousResult?.ToGuid(WholesaleResultColumnNames.ResultId) == currentResult.ToGuid(WholesaleResultColumnNames.ResultId)
            && ResultStartEqualsPreviousResultEnd(currentResult, previousResult);
    }

    protected override string BuildSqlQuery()
    {
        var columnNames = SchemaDefinition.Keys.ToArray();

        var whereStatement = $"WHERE {WholesaleResultColumnNames.CalculationId} = '{CalculationId}'";
        if (!string.IsNullOrEmpty(_energySupplier))
            whereStatement += $" AND {WholesaleResultColumnNames.EnergySupplierId} = '{_energySupplier}'";

        return $"""
                SELECT {string.Join(", ", columnNames)}
                FROM {DatabaseName}.{DataObjectName}
                {whereStatement}
                ORDER BY {WholesaleResultColumnNames.ResultId}, {WholesaleResultColumnNames.Time}
                """;
    }

    private static WholesaleTimeSeriesPoint CreateTimeSeriesPoint(DatabricksSqlRow databricksSqlRow)
    {
        return new WholesaleTimeSeriesPoint(
            databricksSqlRow.ToInstant(WholesaleResultColumnNames.Time),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Quantity),
            QuantityQualitiesMapper.TryFromDeltaTableValue(databricksSqlRow.ToNullableString(WholesaleResultColumnNames.QuantityQualities)),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Price),
            databricksSqlRow.ToNullableDecimal(WholesaleResultColumnNames.Amount));
    }

    /// <summary>
    /// Checks if the current result follows the previous result based on time and resolution.
    /// </summary>
    private bool ResultStartEqualsPreviousResultEnd(DatabricksSqlRow currentResult, DatabricksSqlRow previousResult)
    {
        var endTimeFromPreviousResult = GetEndTimeOfPreviousResult(previousResult);
        var startTimeFromCurrentResult = currentResult.ToInstant(WholesaleResultColumnNames.Time);

        // The start time of the current result should be the same as the end time of the previous result if the result is in sequence with the previous result.
        return endTimeFromPreviousResult == startTimeFromCurrentResult;
    }

    private Instant GetEndTimeOfPreviousResult(DatabricksSqlRow previousResult)
    {
        var resolutionOfPreviousResult = ResolutionMapper.FromDeltaTableValue(
            previousResult.ToNonEmptyString(WholesaleResultColumnNames.Resolution));
        var startTimeOfPreviousResult = previousResult.ToInstant(WholesaleResultColumnNames.Time);

        return PeriodFactory.GetEndDateWithResolutionOffset(resolutionOfPreviousResult, startTimeOfPreviousResult);
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
