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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Activities;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Model;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

internal class EnqueueMessagesOrchestration
{
    [Function(nameof(EnqueueMessagesOrchestration))]
    public async Task<string> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        FunctionContext executionContext)
    {
        var input = context.GetInput<EnqueueMessagesOrchestrationInput>();
        if (input == null)
        {
            return "Error: No input specified.";
        }

        // Fan-out/fan-in => https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-cloud-backup?tabs=csharp
        var tasks = new Task<int>[3];
        tasks[0] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForGridAreaOwnersActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId));

        tasks[1] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId));

        tasks[2] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId));

        await Task.WhenAll(tasks);

        var numberOfEnqueuedMessages = tasks.Sum(t => t.Result);
        var messagesWasSuccessfullyEnqueued = numberOfEnqueuedMessages > 0;

        await context.CallActivityAsync(
            nameof(SendActorMessagesEnqueuedActivity),
            new SendMessagesEnqueuedInput(
                context.InstanceId,
                input.CalculationOrchestrationId,
                input.CalculationId,
                messagesWasSuccessfullyEnqueued));

        return "Success";
    }
}
