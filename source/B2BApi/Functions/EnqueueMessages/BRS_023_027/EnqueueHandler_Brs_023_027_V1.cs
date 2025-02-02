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
/// The <see cref="EnqueueMessagesDto"/><see cref="EnqueueMessagesDto.JsonInput"/> must be of type <see cref="CalculationEnqueueActorMessagesV1"/>.
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

    protected override async Task EnqueueActorMessagesV1Async(
        Guid serviceBusMessageId,
        Guid orchestrationInstanceId,
        EnqueueActorMessagesV1 enqueueActorMessages,
        CancellationToken cancellationToken)
    {
        var featureIsDisabled =
            !await _featureFlagManager.UseProcessManagerToEnqueueBrs023027MessagesAsync().ConfigureAwait(false);

        _logger.LogInformation(
            "Received enqueue actor messages for BRS 023/027. Feature is {Status}. Data: {Data}",
            featureIsDisabled ? "disabled" : "enabled",
            enqueueActorMessages.Data);

        if (featureIsDisabled)
        {
            return;
        }

        switch (enqueueActorMessages.DataType)
        {
            case nameof(CalculationEnqueueActorMessagesV1):
                await HandleCalculationEnqueueActorMessagesV1Async(
                        enqueueActorMessages,
                        serviceBusMessageId)
                    .ConfigureAwait(false);
                break;
            default:
                throw new NotSupportedException($"Data type '{enqueueActorMessages.DataType}' is not supported.");
        }
    }

    private async Task HandleCalculationEnqueueActorMessagesV1Async(EnqueueActorMessagesV1 enqueueActorMessages, Guid eventId)
    {
        var calculatedData = enqueueActorMessages.ParseData<CalculationEnqueueActorMessagesV1>();
        var orchestrationInput = new EnqueueMessagesOrchestrationInput(
            CalculationId: calculatedData.CalculationId,
            CalculationOrchestrationId: enqueueActorMessages.OrchestrationInstanceId,
            EventId: eventId);

        var durableClient = _durableClientFactory.CreateClient();

        var instanceId = await durableClient.StartNewAsync(
            nameof(EnqueueMessagesOrchestration),
            orchestrationInput).ConfigureAwait(false);

        _logger.LogInformation(
            "Started 'EnqueueMessagesOrchestration' (id '{OrchestrationInstanceId}', calculation id: {CalculationId}, calculation orchestration id: {CalculationOrchestrationId}, service bus message id: {EventId}.)",
            instanceId,
            calculatedData.CalculationId,
            enqueueActorMessages.OrchestrationInstanceId,
            eventId);
    }
}
