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

using System.Threading;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class EnqueueEnergyResultsForGridAreaOwnersActivity(
    IServiceScopeFactory serviceScopeFactory,
    IMasterDataClient masterDataClient,
    EnergyResultEnumerator energyResultEnumerator)
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IMasterDataClient _masterDataClient = masterDataClient;
    private readonly EnergyResultEnumerator _energyResultEnumerator = energyResultEnumerator;

    [Function(nameof(EnqueueEnergyResultsForGridAreaOwnersActivity))]
    public async Task<int> Run(
        [ActivityTrigger] EnqueueMessagesInput input)
    {
        try
        {
            var numberOfEnqueuedMessages = 0;

            var query = new EnergyResultPerGridAreaQuery(_energyResultEnumerator.EdiDatabricksOptions, input.CalculationId);
            await foreach (var energyResult in _energyResultEnumerator.GetAsync(query))
            {
                // TODO: It should be possible to implement a cache for grid area owner, so we improve the performance of the loop
                var receiverNumber = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(energyResult.GridAreaCode, CancellationToken.None).ConfigureAwait(false);
                // TODO: It should be possible to create the EnergyResultMessageDto directly in queries
                var energyResultMessage = EnergyResultMessageDtoFactory.Create(EventId.From(input.EventId), energyResult, receiverNumber);

                using var scope = _serviceScopeFactory.CreateScope();
                var scopedClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();
                await scopedClient.EnqueueAndCommitAsync(energyResultMessage, CancellationToken.None).ConfigureAwait(false);

                numberOfEnqueuedMessages++;
            }

            return numberOfEnqueuedMessages;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
