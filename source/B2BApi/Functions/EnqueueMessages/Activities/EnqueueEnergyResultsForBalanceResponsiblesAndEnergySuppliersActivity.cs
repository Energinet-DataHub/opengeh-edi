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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

/// <summary>
/// Enqueue energy results for Balance Responsibles and Energy Suppliers as outgoing messages for the given calculation id.
/// </summary>
public class EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
    ILogger<EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly ILogger<EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity))]
    public async Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        var query = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(
            _logger,
            _energyResultEnumerator.EdiDatabricksOptions,
            EventId.From(input.EventId),
            input.CalculationId);

        _logger.LogInformation(
            "Starting enqueuing messages for energy query, type: {QueryType}, calculation id: {CalculationId}, event id: {EventId}",
            query.GetType().Name,
            input.CalculationId,
            input.EventId);

        var activityStopwatch = Stopwatch.StartNew();
        var databricksStopwatch = Stopwatch.StartNew();
        await foreach (var queryResult in _energyResultEnumerator.GetAsync(query))
        {
            databricksStopwatch.Stop();
            _logger.LogInformation(
                "Retrieved energy result from databricks, elapsed time: {ElapsedTime}, type: {QueryType}, external id: {ExternalId}, calculation id: {CalculationId}",
                databricksStopwatch.Elapsed,
                query.GetType().Name,
                queryResult.Result!.ExternalId.Value,
                input.CalculationId);

            var enqueueStopwatch = Stopwatch.StartNew();
            var enqueueWasSuccess = false;
            if (queryResult.IsSuccess)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        var scopedOutgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
                        await scopedOutgoingMessagesClient.EnqueueAndCommitAsync(queryResult.Result!, CancellationToken.None).ConfigureAwait(false);

                        numberOfHandledResults++;
                        enqueueWasSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        numberOfFailedResults++;
                        _logger.LogWarning(ex, "Enqueue and commit of energy result failed for CalculationId='{CalculationId}'.", input.CalculationId);
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
                logStatusText + " energy result in database, elapsed time: {ElapsedTime}, successful results: {SuccessfulResultsCount}, failed results: {FailedResultsCount}, type: {QueryType}, external id: {ExternalId}, calculation id: {CalculationId}, event id: {EventId}",
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
            "Finished enqueuing messages for energy query, elapsed time: {ElapsedTime}, successful results: {SuccessfulResultsCount}, failed results: {FailedResultsCount}, type: {QueryType}, calculation id: {CalculationId}, event id: {EventId}",
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
