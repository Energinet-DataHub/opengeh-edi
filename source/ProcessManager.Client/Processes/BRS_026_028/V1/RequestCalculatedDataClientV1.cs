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
using Microsoft.Extensions.Azure;

namespace Energinet.DataHub.ProcessManager.Client.Processes.BRS_026_028.V1;

public class RequestCalculatedDataClientV1(
    IAzureClientFactory<ServiceBusSender> serviceBusFactory) : IRequestCalculatedDataClientV1
{
    private readonly ServiceBusSender _serviceBusSender = serviceBusFactory.CreateClient(nameof(ProcessManagerClientOptions.ProcessManagerTopic));

    public async Task RequestCalculatedEnergyTimeSeriesAsync(ExpandoObject input, CancellationToken cancellationToken)
    {
        // TODO: Should input be generic or specific to the process?
        // TODO: Create and send "Start orchestration DTO" protobuf messages
        var jsonMessage = JsonSerializer.Serialize(input);
        ServiceBusMessage serviceBusMessage = new(jsonMessage);
        await _serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RequestCalculatedWholesaleServicesAsync(ExpandoObject input, CancellationToken cancellationToken)
    {
        // TODO: Should input be generic or specific to the process?
        // TODO: Create and send "Start orchestration DTO" protobuf messages
        var jsonMessage = JsonSerializer.Serialize(input);
        ServiceBusMessage serviceBusMessage = new(jsonMessage);
        await _serviceBusSender.SendMessageAsync(serviceBusMessage, cancellationToken)
            .ConfigureAwait(false);
    }
}
