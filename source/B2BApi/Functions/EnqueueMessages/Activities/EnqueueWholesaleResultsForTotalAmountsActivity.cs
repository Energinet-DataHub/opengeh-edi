﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class EnqueueWholesaleResultsForTotalAmountsActivity(
    ILogger<EnqueueWholesaleResultsForTotalAmountsActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    WholesaleResultEnumerator wholesaleResultEnumerator)
{
    private readonly ILogger<EnqueueWholesaleResultsForTotalAmountsActivity> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly WholesaleResultEnumerator _wholesaleResultEnumerator = wholesaleResultEnumerator;

    [Function(nameof(EnqueueWholesaleResultsForTotalAmountsActivity))]
    public async Task<int> Run([ActivityTrigger] EnqueueMessagesInput input)
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        var query = new WholesaleTotalAmountQuery(
            _logger,
            _wholesaleResultEnumerator.EdiDatabricksOptions,
            EventId.From(input.EventId),
            input.CalculationId);

        _logger.LogInformation(
            "Starting enqueuing messages for wholesale query, type: {QueryType}, calculation id: {CalculationId}, event id: {EventId}",
            query.GetType().Name,
            input.CalculationId,
            input.EventId);

        var activityStopwatch = Stopwatch.StartNew();
        var databricksStopwatch = Stopwatch.StartNew();
        await foreach (var queryResult in _wholesaleResultEnumerator.GetAsync(query))
        {
            databricksStopwatch.Stop();
            _logger.LogInformation(
                "Retrieved wholesale result from databricks, elapsed time: {ElapsedTime}, type: {QueryType}, external id: {ExternalId}, calculation id: {CalculationId}, event id: {EventId}",
                databricksStopwatch.Elapsed,
                query.GetType().Name,
                queryResult.Result!.ExternalId.Value,
                input.CalculationId,
                input.EventId);

            var enqueueStopwatch = Stopwatch.StartNew();
            var enqueueWasSuccess = false;
            if (queryResult.IsSuccess)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        var scopedOutgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
                        await scopedOutgoingMessagesClient.EnqueueAndCommitAsync(queryResult.Result, CancellationToken.None).ConfigureAwait(false);

                        numberOfHandledResults++;
                        enqueueWasSuccess = true;
                    }
                    catch
                    {
                        numberOfFailedResults++;
                    }
                }
            }
            else
            {
                numberOfFailedResults++;
            }

            enqueueStopwatch.Stop();
            var logStatusText = enqueueWasSuccess ? "Successfully enqueued" : "Failed enqueuing";
            _logger.LogInformation(
                logStatusText + " wholesale result in database, elapsed time: {ElapsedTime}, successful results: {SuccessfulResultsCount}, failed results: {FailedResultsCount}, type: {QueryType}, external id: {ExternalId}, calculation id: {CalculationId}, event id: {EventId}",
                enqueueStopwatch.Elapsed.ToString(),
                numberOfHandledResults,
                numberOfFailedResults,
                query.GetType().Name,
                queryResult.Result.ExternalId.Value,
                input.CalculationId,
                input.EventId);
            databricksStopwatch.Restart();
        }

        _logger.LogInformation(
            "Finished enqueuing messages for wholesale query, elapsed time: {ElapsedTime}, successful results: {SuccessfulResultsCount}, failed results: {FailedResultsCount}, type: {QueryType}, calculation id: {CalculationId}, event id: {EventId}",
            activityStopwatch.Elapsed,
            numberOfHandledResults,
            numberOfFailedResults,
            query.GetType().Name,
            input.CalculationId,
            input.EventId);

        return numberOfFailedResults > 0
            ? throw new Exception($"Enqueue messages activity failed. CalculationId='{input.CalculationId}' EventId='{input.EventId}' NumberOfFailedResults='{numberOfFailedResults}'")
            : numberOfHandledResults;
    }
}