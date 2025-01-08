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

using System.Diagnostics;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021_023.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021_023.Activities;

public abstract class EnqueueWholesaleResultsBaseActivity(
    ILogger logger,
    IServiceScopeFactory serviceScopeFactory,
    WholesaleResultEnumerator wholesaleResultEnumerator)
{
    private readonly ILogger _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly WholesaleResultEnumerator _wholesaleResultEnumerator = wholesaleResultEnumerator;

    protected async Task<int> EnqueueWholesaleResults<TQueryResult>(EnqueueMessagesForActorInput input, WholesaleResultQueryBase<TQueryResult> query)
        where TQueryResult : OutgoingMessageDto
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        _logger.LogInformation(
            "Starting enqueuing messages for wholesale query, type: {QueryType}, actor: {Actor}, calculation id: {CalculationId}, event id: {EventId}",
            query.GetType().Name,
            input.Actor,
            input.CalculationId,
            input.EventId);

        var activityStopwatch = Stopwatch.StartNew();
        var databricksStopwatch = Stopwatch.StartNew();
        await foreach (var queryResult in _wholesaleResultEnumerator.GetAsync(query))
        {
            databricksStopwatch.Stop();
            // Only log databricks query time if it took more than 1 second
            if (databricksStopwatch.Elapsed > TimeSpan.FromSeconds(1))
            {
                _logger.LogInformation(
                    "Retrieved energy result from databricks, elapsed time: {ElapsedTime}, type: {QueryType}, external id: {ExternalId}, actor: {Actor}, calculation id: {CalculationId}, event id: {EventId}",
                    databricksStopwatch.Elapsed,
                    query.GetType().Name,
                    queryResult.Result?.ExternalId.Value,
                    input.Actor,
                    input.CalculationId,
                    input.EventId);
            }

            if (queryResult.IsSuccess)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                try
                {
                    var scopedOutgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
                    await EnqueueAndCommitWholesaleResult(scopedOutgoingMessagesClient, queryResult.Result!).ConfigureAwait(false);

                    numberOfHandledResults++;
                }
                catch (Exception ex)
                {
                    numberOfFailedResults++;
                    _logger.LogWarning(
                        ex,
                        "Enqueue and commit threw exception for wholesale result, query type: {QueryType}, external id: {ExternalId}, actor: {Actor}, calculation id: {CalculationId}, event id: {EventId}",
                        query.GetType().Name,
                        queryResult.Result?.ExternalId.Value,
                        input.Actor,
                        input.CalculationId,
                        input.EventId);
                }
            }
            else
            {
                numberOfFailedResults++;
            }

            databricksStopwatch.Restart();
        }

        _logger.LogInformation(
            "Finished enqueuing messages for wholesale query, elapsed time: {ElapsedTime}, successful results: {SuccessfulResultsCount}, failed results: {FailedResultsCount}, type: {QueryType}, actor: {Actor}, calculation id: {CalculationId}, event id: {EventId}",
            activityStopwatch.Elapsed,
            numberOfHandledResults,
            numberOfFailedResults,
            query.GetType().Name,
            input.Actor,
            input.CalculationId,
            input.EventId);

        return numberOfFailedResults > 0
            ? throw new Exception($"Enqueue messages activity failed. Actor='{input.Actor}' CalculationId='{input.CalculationId}' EventId='{input.EventId}' NumberOfFailedResults='{numberOfFailedResults}' NumberOfHandledResults='{numberOfHandledResults}'")
            : numberOfHandledResults;
    }

    protected abstract Task EnqueueAndCommitWholesaleResult<T>(IOutgoingMessagesClient outgoingMessagesClient, T queryResult)
        where T : OutgoingMessageDto;
}
