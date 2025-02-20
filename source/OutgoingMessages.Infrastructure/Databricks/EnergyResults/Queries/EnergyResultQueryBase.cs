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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableMappers;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;

public abstract class EnergyResultQueryBase<TResult>(
        ILogger logger,
        EdiDatabricksOptions ediDatabricksOptions,
        Guid calculationId)
    : CalculationResultQueryBase<TResult>(ediDatabricksOptions, calculationId)
    where TResult : OutgoingMessageDto
{
    private readonly ILogger _logger = logger;

    protected abstract Task<TResult> CreateEnergyResultAsync(
        DatabricksSqlRow databricksSqlRow,
        IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints);

    protected override async Task<QueryResult<TResult>> CreateResultAsync(
        List<DatabricksSqlRow> currentResultSet)
    {
        var firstRow = currentResultSet.First();
        var resultId = firstRow.ToGuid(EnergyResultColumnNames.ResultId);

        try
        {
            var timeSeriesPoints = new List<EnergyTimeSeriesPoint>();

            foreach (var row in currentResultSet)
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

    protected override bool BelongsToSameResultSet(DatabricksSqlRow currentResult, DatabricksSqlRow? previousResult)
    {
        return
            previousResult?.ToGuid(EnergyResultColumnNames.ResultId) == currentResult.ToGuid(EnergyResultColumnNames.ResultId)
            && ResultStartEqualsPreviousResultEnd(currentResult, previousResult);
    }

    protected override string BuildSqlQuery()
    {
        var columnNames = SchemaDefinition.Keys.ToArray();

        return $"""
                SELECT {string.Join(", ", columnNames)}
                FROM {DatabaseName}.{DataObjectName}
                WHERE {EnergyResultColumnNames.CalculationId} = '{CalculationId}'
                ORDER BY {EnergyResultColumnNames.ResultId}, {EnergyResultColumnNames.Time}
                """;
    }

    private static EnergyTimeSeriesPoint CreateTimeSeriesPoint(DatabricksSqlRow databricksSqlRow)
    {
        return new EnergyTimeSeriesPoint(
            databricksSqlRow.ToInstant(EnergyResultColumnNames.Time),
            databricksSqlRow.ToDecimal(EnergyResultColumnNames.Quantity),
            QuantityQualityMapper.FromDeltaTableValues(databricksSqlRow.ToNonEmptyString(EnergyResultColumnNames.QuantityQualities)));
    }

    /// <summary>
    /// Checks if the current result follows the previous result based on time and resolution.
    /// </summary>
    private bool ResultStartEqualsPreviousResultEnd(DatabricksSqlRow currentResult, DatabricksSqlRow previousResult)
    {
        var endTimeFromPreviousResult = GetEndTimeOfPreviousResult(previousResult);
        var startTimeFromCurrentResult = currentResult.ToInstant(EnergyResultColumnNames.Time);

        // The start time of the current result should be the same as the end time of the previous result if the result is in sequence with the previous result.
        return endTimeFromPreviousResult == startTimeFromCurrentResult;
    }

    private Instant GetEndTimeOfPreviousResult(DatabricksSqlRow previousResult)
    {
        var resolutionOfPreviousResult =
            ResolutionMapper.FromDeltaTableValue(
                previousResult.ToNonEmptyString(EnergyResultColumnNames.Resolution));
        var startTimeOfPreviousResult = previousResult.ToInstant(EnergyResultColumnNames.Time);

        return PeriodFactory.GetEndDateWithResolutionOffset(
            resolutionOfPreviousResult,
            startTimeOfPreviousResult);
    }
}
