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

public class EnqueueWholesaleResultsForAmountPerChargesActivity(
    IServiceScopeFactory serviceScopeFactory,
    WholesaleResultEnumerator wholesaleResultEnumerator)
{
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly WholesaleResultEnumerator _wholesaleResultEnumerator = wholesaleResultEnumerator;

    [Function(nameof(EnqueueWholesaleResultsForAmountPerChargesActivity))]
    public async Task<int> Run(
    [ActivityTrigger] EnqueueMessagesInput input)
    {
        var numberOfHandledResults = 0;
        var numberOfFailedResults = 0;

        var query = new WholesaleAmountPerChargeQuery(_wholesaleResultEnumerator.EdiDatabricksOptions, input.CalculationId);
        await foreach (var wholesaleResult in _wholesaleResultEnumerator.GetAsync(query))
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                try
                {
                    var scopedMasterDataClient = scope.ServiceProvider.GetRequiredService<IMasterDataClient>();
                    var scopedOutgoingMessagesClient = scope.ServiceProvider.GetRequiredService<IOutgoingMessagesClient>();

                    // TODO: It should be possible to implement a cache for grid area owner, so we improve the performance of the loop
                    var gridOwner = await scopedMasterDataClient.GetGridOwnerForGridAreaCodeAsync(wholesaleResult.GridAreaCode, CancellationToken.None).ConfigureAwait(false);
                    // TODO: It should be possible to create the WholesaleServicesMessageDto directly in queries
                    var wholesaleResultMessage = WholesaleResultMessageDtoFactory.Create(EventId.From(input.EventId), wholesaleResult, gridOwner);
                    await scopedOutgoingMessagesClient.EnqueueAndCommitAsync(wholesaleResultMessage, CancellationToken.None).ConfigureAwait(false);

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
