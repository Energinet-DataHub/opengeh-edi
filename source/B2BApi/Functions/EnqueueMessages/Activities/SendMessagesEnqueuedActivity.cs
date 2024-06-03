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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EnergySupplying.RequestResponse.IntegrationEvents;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;

public class SendMessagesEnqueuedActivity
{
    private readonly IServiceBusSenderAdapter _senderCreator;

    public SendMessagesEnqueuedActivity(
        IServiceBusSenderFactory serviceBusSenderFactory,
        IOptions<WholesaleInboxOptions> options)
    {
        ArgumentNullException.ThrowIfNull(serviceBusSenderFactory);
        ArgumentNullException.ThrowIfNull(options);

        _senderCreator = serviceBusSenderFactory.GetSender(options.Value.QueueName);
    }

    [Function(nameof(SendMessagesEnqueuedActivity))]
    public async Task Run(
        [ActivityTrigger] SendMessagesEnqueuedInput input)
    {
        var messagesEnqueuedEvent = new ActorMessagesEnqueuedV1
        {
            OrchestrationInstanceId = input.CalculationOrchestrationInstanceId,
            CalculationId = input.CalculationId,
            Success = input.Success,
        };

        var eventId = Guid.Parse(input.OrchestrationInstanceId);
        var serviceBusMessage = CreateServiceBusMessage(messagesEnqueuedEvent, eventId);

        await _senderCreator.SendAsync(serviceBusMessage, CancellationToken.None);
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
        return serviceBusMessage;
    }
}
