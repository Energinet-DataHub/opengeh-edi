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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EnergySupplying.RequestResponse.IntegrationEvents;
using Energinet.DataHub.ProcessManager.Abstractions.Api.Model;
using Energinet.DataHub.ProcessManager.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_023_027.Activities;

public class SendActorMessagesEnqueuedActivity
{
    private readonly ServiceBusSender _wholesaleSender;
    private readonly IFeatureFlagManager _featureFlagManager;
    private readonly IProcessManagerMessageClient _processManagerMessageClient;

    public SendActorMessagesEnqueuedActivity(
        IOptions<WholesaleInboxQueueOptions> options,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        IProcessManagerMessageClient processManagerMessageClient,
        IFeatureFlagManager featureFlagManager)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(senderFactory);

        _wholesaleSender = senderFactory.CreateClient(options.Value.QueueName);
        _processManagerMessageClient = processManagerMessageClient;
        _featureFlagManager = featureFlagManager;
    }

    [Function(nameof(SendActorMessagesEnqueuedActivity))]
    public async Task Run(
        [ActivityTrigger] SendMessagesEnqueuedInput input)
    {
        var informProcessManger = await _featureFlagManager
            .UseProcessManagerToEnqueueBrs023027MessagesAsync().ConfigureAwait(false);

        if (informProcessManger)
        {
            await SendToProcessManager(input).ConfigureAwait(false);
        }

        if (!informProcessManger)
        {
            await SendToWholesale(input).ConfigureAwait(false);
        }
    }

    private static ServiceBusMessage CreateServiceBusMessage(ActorMessagesEnqueuedV1 messagesEnqueuedEvent, Guid eventId)
    {
        var serviceBusMessage = new ServiceBusMessage
        {
            Body = new BinaryData(messagesEnqueuedEvent.ToByteArray()),
            Subject = ActorMessagesEnqueuedV1.EventName,
            MessageId = eventId.ToString(),
        };

        serviceBusMessage.ApplicationProperties.Add("EventMinorVersion", ActorMessagesEnqueuedV1.CurrentMinorVersion);
        serviceBusMessage.ApplicationProperties.Add("ReferenceId", eventId.ToString());
        return serviceBusMessage;
    }

    private async Task SendToWholesale(SendMessagesEnqueuedInput input)
    {
        var messagesEnqueuedEvent = new ActorMessagesEnqueuedV1
        {
            OrchestrationInstanceId = input.CalculationOrchestrationInstanceId,
            CalculationId = input.CalculationId.ToString(),
            Success = input.Success,
        };

        var eventId = Guid.Parse(input.OrchestrationInstanceId);
        var serviceBusMessage = CreateServiceBusMessage(messagesEnqueuedEvent, eventId);

        await _wholesaleSender.SendMessageAsync(serviceBusMessage, CancellationToken.None).ConfigureAwait(false);
    }

    private async Task SendToProcessManager(SendMessagesEnqueuedInput input)
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
