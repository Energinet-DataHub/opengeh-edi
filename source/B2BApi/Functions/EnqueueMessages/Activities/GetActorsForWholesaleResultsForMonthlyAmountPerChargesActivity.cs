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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

/// <summary>
/// Enqueue wholesale results for Monthly Amount Per Charge to Energy Supplier and ChargeOwner as outgoing messages for the given calculation id.
/// </summary>
public class GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity(
    ILogger<GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity> logger,
    IServiceScopeFactory serviceScopeFactory,
    IMasterDataClient masterDataClient,
    WholesaleResultActorsEnumerator wholesaleResultActorsEnumerator)
{
    private readonly ILogger<GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IMasterDataClient _masterDataClient = masterDataClient;
    private readonly WholesaleResultActorsEnumerator _wholesaleResultActorsEnumerator = wholesaleResultActorsEnumerator;

    /// <summary>
    /// Start an EnqueueWholesaleResultsForMonthlyAmountPerCharges activity.
    /// <remarks>The <paramref name="input"/> type and return type must be that same as the <see cref="Run"/> method</remarks>
    /// <remarks>Changing the <paramref name="input"/> or return type might break the Durable Function's deserialization</remarks>
    /// </summary>
    public static Task<IReadOnlyCollection<string>> StartActivityAsync(EnqueueMessagesInput input, TaskOrchestrationContext context, TaskOptions? options)
    {
        return context.CallActivityAsync<IReadOnlyCollection<string>>(
            nameof(GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: options);
    }

    [Function(nameof(GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity))]
    public async Task<IReadOnlyCollection<string>> Run([ActivityTrigger] EnqueueMessagesInput input)
    {
        var query = new WholesaleMonthlyAmountPerChargeQuery(
            _logger,
            _wholesaleResultActorsEnumerator.EdiDatabricksOptions,
            _masterDataClient,
            EventId.From(input.EventId),
            input.CalculationId,
            null);

        var actors = await _wholesaleResultActorsEnumerator
            .GetActorsAsync(query)
            .ToListAsync()
            .ConfigureAwait(false);

        return actors;
    }
}
