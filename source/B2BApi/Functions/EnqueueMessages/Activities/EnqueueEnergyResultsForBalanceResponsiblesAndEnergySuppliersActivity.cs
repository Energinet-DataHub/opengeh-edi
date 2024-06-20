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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

/// <summary>
/// Enqueue energy results for Balance Responsibles and Energy Suppliers as outgoing messages for the given calculation id.
/// </summary>
public class EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity(
    IServiceScopeFactory serviceScopeFactory,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity))]
    public async Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        var query = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(
            _energyResultEnumerator.EdiDatabricksOptions,
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
                catch
                {
                    numberOfFailedResults++;
                }
            }
        }

        return numberOfFailedResults > 0
            ? throw new Exception($"Enqueue messages activity failed. CalculationId='{input.CalculationId}' EventId='{input.EventId}' NumberOfFailedResults='{numberOfFailedResults}'")
            : numberOfHandledResults;
    }
}
