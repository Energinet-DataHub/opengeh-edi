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
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;

public static class EnqueueBrs023_027MessageFactory
{
    private static readonly string _orchestrationName = Brs_023_027.Name;

    public static ServiceBusMessage CreateEnqueue(Guid calculationId)
    {
        return CreateServiceBusMessage(new CalculationEnqueueActorMessagesV1(calculationId));
    }

    private static ServiceBusMessage CreateServiceBusMessage<TData>(
        TData data)
            where TData : class
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = _orchestrationName,
            OrchestrationVersion = 1,
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };

        enqueueActorMessages.SetData(data);

        return enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(_orchestrationName),
            idempotencyKey: Guid.NewGuid().ToString());
    }
}
