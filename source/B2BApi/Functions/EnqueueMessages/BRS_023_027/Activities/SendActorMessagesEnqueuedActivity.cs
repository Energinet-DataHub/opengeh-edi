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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Model;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Activities;

public class SendActorMessagesEnqueuedActivity
{
    private readonly IProcessManagerMessageClient _processManagerMessageClient;

    public SendActorMessagesEnqueuedActivity(
        IAzureClientFactory<ServiceBusSender> senderFactory,
        IProcessManagerMessageClient processManagerMessageClient)
    {
        ArgumentNullException.ThrowIfNull(senderFactory);

        _processManagerMessageClient = processManagerMessageClient;
    }

    [Function(nameof(SendActorMessagesEnqueuedActivity))]
    public async Task Run(
        [ActivityTrigger] SendMessagesEnqueuedInput input)
    {
        await _processManagerMessageClient.NotifyOrchestrationInstanceAsync(
                new NotifyOrchestrationInstanceEvent<CalculationEnqueueActorMessagesCompletedNotifyEventV1>(
                    OrchestrationInstanceId: input.CalculationOrchestrationInstanceId,
                    EventName: CalculationEnqueueActorMessagesCompletedNotifyEventV1.EventName,
                    Data: new CalculationEnqueueActorMessagesCompletedNotifyEventV1 { Success = input.Success }),
                CancellationToken.None)
            .ConfigureAwait(false);
    }
}
