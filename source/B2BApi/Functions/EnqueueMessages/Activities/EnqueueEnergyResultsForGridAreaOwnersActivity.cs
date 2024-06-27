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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

/// <summary>
/// Enqueue energy results for Grid Area Owners as outgoing messages for the given calculation id.
/// </summary>
public class EnqueueEnergyResultsForGridAreaOwnersActivity(
    ILogger<EnqueueEnergyResultsForGridAreaOwnersActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    IMasterDataClient masterDataClient,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly ILogger<EnqueueEnergyResultsForGridAreaOwnersActivity> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IMasterDataClient _masterDataClient = masterDataClient;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueEnergyResultsForGridAreaOwnersActivity))]
    public async Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        var query = new EnergyResultPerGridAreaQuery(
            _logger,
            _energyResultEnumerator.EdiDatabricksOptions,
            _masterDataClient,
            EventId.From(input.EventId),
            input.CalculationId);
        await foreach (var energyResult in _energyResultEnumerator.GetAsync(query))
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    var scopedOutgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
                    // TODO: Did not use returned "numberOfEnqueuedMessages", let's talk
                    await scopedOutgoingMessagesClient.EnqueueAndCommitAsync(energyResult, CancellationToken.None).ConfigureAwait(false);

                    numberOfHandledResults++;
                }
                catch (Exception ex)
                {
                    numberOfFailedResults++;
                    _logger.LogWarning(ex, "Enqueue and commit of energy result failed for CalculationId='{CalculationId}'.", input.CalculationId);
                }
            }
        }

        return numberOfFailedResults > 0
            ? throw new Exception($"Enqueue messages activity failed. CalculationId='{input.CalculationId}' EventId='{input.EventId}' NumberOfFailedResults='{numberOfFailedResults}'")
            : numberOfHandledResults;
    }
}
