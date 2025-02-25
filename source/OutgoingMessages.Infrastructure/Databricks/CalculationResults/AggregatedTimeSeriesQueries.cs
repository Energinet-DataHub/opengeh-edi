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
using Energinet.DataHub.EDI.OutgoingMessages.Application.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;

public class AggregatedTimeSeriesQueries(
    DatabricksSqlWarehouseQueryExecutor databricksSqlWarehouseQueryExecutor,
    AggregatedTimeSeriesQuerySnippetProviderFactory querySnippetProviderFactory,
    IOptions<DeltaTableOptions> deltaTableOptions)
    : RequestQueriesBase(databricksSqlWarehouseQueryExecutor), IAggregatedTimeSeriesQueries
{
    private readonly AggregatedTimeSeriesQuerySnippetProviderFactory _querySnippetProviderFactory =
        querySnippetProviderFactory;

    private readonly IOptions<DeltaTableOptions> _deltaTableOptions = deltaTableOptions;

    public async IAsyncEnumerable<AggregatedTimeSeries> GetAsync(AggregatedTimeSeriesQueryParameters parameters)
    {
        var aggregationLevel = AggregationLevelMapper.Map(
            parameters.MeteringPointType,
            parameters.SettlementMethod,
            parameters.EnergySupplierId,
            parameters.BalanceResponsibleId);

        var querySnippetProvider = _querySnippetProviderFactory.Create(parameters, aggregationLevel);

        var calculationTypePerGridAreas =
            await GetCalculationTypeForGridAreasAsync(
                    querySnippetProvider.DatabricksContract.GetGridAreaCodeColumnName(),
                    querySnippetProvider.DatabricksContract.GetCalculationTypeColumnName(),
                    new AggregatedTimeSeriesCalculationTypeForGridAreasQueryStatement(
                        _deltaTableOptions.Value,
                        querySnippetProvider),
                    parameters.BusinessReason,
                    parameters.SettlementVersion)
                .ConfigureAwait(false);

        var sqlStatement = new AggregatedTimeSeriesQueryStatement(
            calculationTypePerGridAreas,
            querySnippetProvider,
            _deltaTableOptions.Value);

        var calculationIdColumnName = querySnippetProvider.DatabricksContract.GetCalculationIdColumnName();

        await foreach (var aggregatedTimeSeries in CreateSeriesPackagesAsync(
                           (row, points) => AggregatedTimeSeriesFactory.Create(
                               querySnippetProvider.DatabricksContract,
                               row,
                               points),
                           (currentRow, previousRow) =>
                               querySnippetProvider.DatabricksContract.GetColumnsToAggregateBy()
                                   .Any(column => currentRow[column] != previousRow[column])
                               || currentRow[calculationIdColumnName] != previousRow[calculationIdColumnName]
                               || !ResultStartEqualsPreviousResultEnd(
                                   currentRow,
                                   previousRow,
                                   querySnippetProvider.DatabricksContract),
                           EnergyTimeSeriesPointFactory.CreateTimeSeriesPoint,
                           sqlStatement))
        {
            yield return aggregatedTimeSeries;
        }
    }

    /// <summary>
    /// Checks if the current result follows the previous result based on time and resolution.
    /// </summary>
    private bool ResultStartEqualsPreviousResultEnd(
        DatabricksSqlRow currentResult,
        DatabricksSqlRow previousResult,
        IAggregatedTimeSeriesDatabricksContract databricksContract)
    {
        var endTimeFromPreviousResult = GetEndTimeOfPreviousResult(previousResult, databricksContract);
        var startTimeFromCurrentResult = SqlResultValueConverters
            .ToDateTimeOffset(currentResult[databricksContract.GetTimeColumnName()])!.Value;

        // The start time of the current result should be the same as the end time of the previous result if the result is in sequence with the previous result.
        return endTimeFromPreviousResult == startTimeFromCurrentResult;
    }

    private DateTimeOffset GetEndTimeOfPreviousResult(
        DatabricksSqlRow previousResult,
        IAggregatedTimeSeriesDatabricksContract databricksContract)
    {
        var resolutionOfPreviousResult = ResolutionMapper
            .FromDeltaTableValue(previousResult[databricksContract.GetResolutionColumnName()]!);

        var startTimeOfPreviousResult = SqlResultValueConverters
            .ToDateTimeOffset(previousResult[databricksContract.GetTimeColumnName()])!.Value;

        return PeriodHelper.GetDateTimeWithResolutionOffset(
            resolutionOfPreviousResult,
            startTimeOfPreviousResult);
    }
}
