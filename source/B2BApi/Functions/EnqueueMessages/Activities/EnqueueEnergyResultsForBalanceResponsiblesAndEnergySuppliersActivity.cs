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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

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
        try
        {
            var numberOfEnqueuedMessages = 0;

            var query = new EnergyResultPerEnergySupplierBrpGridAreaQuery(_energyResultEnumerator.EdiDatabricksOptions, input.CalculationId);
            await foreach (var energyResult in _energyResultEnumerator.GetAsync(query))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var scopedClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();

                    // TODO: It should be possible to create the EnergyResultMessageDto directly in queries
                    var energyResultPerEnergySupplierMessage = EnergyResultMessageDtoFactory.CreateForEnergySupplier(EventId.From(input.EventId), energyResult);
                    await scopedClient.EnqueueAndCommitAsync(energyResultPerEnergySupplierMessage, CancellationToken.None).ConfigureAwait(false);
                    numberOfEnqueuedMessages++;

                    var energyResultPerBalanceResponsibleMessage = EnergyResultMessageDtoFactory.CreateForBalanceResponsiblePrEnergySupplier(EventId.From(input.EventId), energyResult);
                    await scopedClient.EnqueueAndCommitAsync(energyResultPerBalanceResponsibleMessage, CancellationToken.None).ConfigureAwait(false);
                    numberOfEnqueuedMessages++;
                }
            }

            return numberOfEnqueuedMessages;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
