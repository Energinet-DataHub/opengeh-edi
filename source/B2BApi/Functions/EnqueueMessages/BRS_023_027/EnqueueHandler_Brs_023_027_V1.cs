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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Model;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027;

/// <summary>
/// Enqueue messages for BRS-023 (NotifyAggregatedMeasureData) and BRS-027 (NotifyWholesaleServices).
/// The <see cref="EnqueueMessagesDto"/><see cref="EnqueueMessagesDto.JsonInput"/> must be of type <see cref="CalculatedDataForCalculationTypeV1"/>.
/// </summary>
/// <param name="logger"></param>
public class EnqueueHandler_Brs_023_027_V1(
    ILogger<EnqueueHandler_Brs_023_027_V1> logger,
    IFeatureFlagManager featureFlagManager,
    IDurableClientFactory durableClientFactory)
    : EnqueueActorMessagesHandlerBase(logger)
{
    private readonly ILogger _logger = logger;

    private readonly IFeatureFlagManager _featureFlagManager = featureFlagManager;

    private readonly IDurableClientFactory _durableClientFactory = durableClientFactory;

    protected override async Task EnqueueActorMessagesV1Async(EnqueueActorMessagesV1 enqueueActorMessages)
    {
        if (!await _featureFlagManager.EnqueueBrs023027MessagesViaProcessManagerAsync().ConfigureAwait(false))
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Received enqueue actor messages for BRS 023/027. Data: {Data}",
            enqueueActorMessages.Data);

        var calculationCompleted = enqueueActorMessages.ParseData<CalculatedDataForCalculationTypeV1>();
        var orchestrationInput = new EnqueueMessagesOrchestrationInput(
            CalculationId: calculationCompleted.CalculationId,
            CalculationOrchestrationId: enqueueActorMessages.OrchestrationInstanceId,
            // This is not event driven anymore. But it is still passed to the outgoing messages
            // Do we want to preserve this and use the orchestration instance id instead?
            // This should be a guid, even tho the datatype states otherwise
            EventId: Guid.Parse(enqueueActorMessages.OrchestrationInstanceId));

        var durableClient = _durableClientFactory.CreateClient();

        var instanceId = await durableClient.StartNewAsync(
            nameof(EnqueueMessagesOrchestration),
            orchestrationInput).ConfigureAwait(false);

        _logger.LogInformation(
            "Started 'EnqueueMessagesOrchestration' (id '{OrchestrationInstanceId}', calculation id: {CalculationId}, calculation type: {CalculationType}, calculation orchestration id: {CalculationOrchestrationId}.",
            instanceId,
            calculationCompleted.CalculationId,
            calculationCompleted.CalculationType,
            enqueueActorMessages.OrchestrationInstanceId);

        // TODO: Call actual logic that enqueues messages (starts orchestration)
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
