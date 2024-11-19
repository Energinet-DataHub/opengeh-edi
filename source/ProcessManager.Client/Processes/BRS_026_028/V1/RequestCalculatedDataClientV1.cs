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

using System.Dynamic;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using Energinet.DataHub.ProcessManager.Client.Processes.BRS_026_028.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_026.V1.Models;
using Google.Protobuf;
using Microsoft.Extensions.Azure;

namespace Energinet.DataHub.ProcessManager.Client.Processes.BRS_026_028.V1;

public class RequestCalculatedDataClientV1(
    IAzureClientFactory<ServiceBusSender> serviceBusFactory) : IRequestCalculatedDataClientV1
{
    private readonly ServiceBusSender _serviceBusSender = serviceBusFactory.CreateClient(nameof(ProcessManagerServiceBusClientsOptions.ProcessManagerTopic));

    public async Task RequestCalculatedEnergyTimeSeriesAsync(RequestCalculatedDataInputV1<RequestCalculatedEnergyTimeSeriesInputV1> input, CancellationToken cancellationToken)
    {
        var serviceBusMessage = CreateServiceBusMessage(
            "BRS_026",
            1,
            input);

        await SendServiceBusMessage(
                serviceBusMessage,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequestCalculatedWholesaleServicesAsync(RequestCalculatedDataInputV1<object> input, CancellationToken cancellationToken)
    {
        var serviceBusMessage = CreateServiceBusMessage(
            "BRS_028",
            1,
            input);

        await SendServiceBusMessage(serviceBusMessage, cancellationToken)
            .ConfigureAwait(false);
    }

    private ServiceBusMessage CreateServiceBusMessage<TInput>(
        string orchestrationName,
        int orchestrationVersion,
        RequestCalculatedDataInputV1<TInput> input)
    where TInput : class
    {
        var message = new StartOrchestrationDto
        {
            OrchestrationName = orchestrationName,
            OrchestrationVersion = orchestrationVersion,
            JsonInput = JsonSerializer.Serialize(input.Input),
        };

        ServiceBusMessage serviceBusMessage = new(JsonFormatter.Default.Format(message))
        {
            Subject = orchestrationName,
            MessageId = input.MessageId,
            ContentType = "application/json",
        };

        return serviceBusMessage;
    }

    private async Task SendServiceBusMessage(
        ServiceBusMessage serviceBusMessage,
        CancellationToken cancellationToken)
    {
        await _serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken)
            .ConfigureAwait(false);
    }
}
