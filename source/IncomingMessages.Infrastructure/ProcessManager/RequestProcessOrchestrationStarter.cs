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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_028.V1.Model;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;

public class RequestProcessOrchestrationStarter(
    IProcessManagerMessageClient processManagerMessageClient,
    AuthenticatedActor authenticatedActor) : IRequestProcessOrchestrationStarter
{
    private readonly IProcessManagerMessageClient _processManagerMessageClient = processManagerMessageClient;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;

    public async Task StartRequestWholesaleServicesOrchestrationAsync(
        InitializeWholesaleServicesProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedActorId(initializeProcessDto.MessageId);
        var actorIdentity = new ActorIdentityDto(actorId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var startCommand = new RequestCalculatedWholesaleServicesCommandV1(
                actorIdentity,
                new RequestCalculatedWholesaleServicesInputV1(),
                transaction.Id);

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    public async Task StartRequestAggregatedMeasureDataOrchestrationAsync(
        InitializeAggregatedMeasureDataProcessDto initializeProcessDto,
        CancellationToken cancellationToken)
    {
        var actorId = GetAuthenticatedActorId(initializeProcessDto.MessageId);
        var actorIdentity = new ActorIdentityDto(actorId);

        var startProcessTasks = new List<Task>();
        foreach (var transaction in initializeProcessDto.Series)
        {
            var startCommand = new StartRequestCalculatedEnergyTimeSeriesCommandV1(
                actorIdentity,
                new RequestCalculatedEnergyTimeSeriesInputV1(
                    BusinessReason: initializeProcessDto.BusinessReason),
                transaction.Id.Value);

            // TODO: Handle resiliency. Could use something like Polly to retry if failing?
            var startProcessTask = _processManagerMessageClient.StartNewOrchestrationInstanceAsync(startCommand, cancellationToken);
            startProcessTasks.Add(startProcessTask);
        }

        await Task.WhenAll(startProcessTasks).ConfigureAwait(false);
    }

    private Guid GetAuthenticatedActorId(string messageId)
    {
        if (!_authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity))
            throw new InvalidOperationException($"Cannot get current actor when initializing process (MessageId={messageId})");

        return actorIdentity?.ActorId
               ?? throw new InvalidOperationException($"Current actor id was null when initializing process (MessageId={messageId})");
    }
}
