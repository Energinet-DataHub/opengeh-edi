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

using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.DurableTask;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Activities;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1;

// TODO: Implement according to guidelines: https://energinet.atlassian.net/wiki/spaces/D3/pages/824803345/Durable+Functions+Development+Guidelines
internal class NotifyAggregatedMeasureDataOrchestrationV1
{
    internal const int CalculationStepIndex = 0;
    internal const int EnqueueMessagesStepIndex = 1;

    [Function(nameof(NotifyAggregatedMeasureDataOrchestrationV1))]
    public async Task<string> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetOrchestrationParameterValue<NotifyAggregatedMeasureDataInputV1>();
        if (input == null)
            return "Error: No input specified.";

        var defaultRetryOptions = CreateDefaultRetryOptions();

        // Initialize
        await context.CallActivityAsync(
            nameof(Brs023OrchestrationInitializeActivityV1),
            context.InstanceId,
            defaultRetryOptions);

        // Step: Calculation
        await context.CallActivityAsync(
            nameof(Brs023CalculationStepStartActivityV1),
            context.InstanceId,
            defaultRetryOptions);
        await context.CallActivityAsync(
            nameof(Brs023CalculationStepTerminateActivityV1),
            context.InstanceId,
            defaultRetryOptions);

        // Step: Enqueue messages
        await context.CallActivityAsync(
            nameof(Brs023EnqueueMessagesStepStartActivityV1),
            context.InstanceId,
            defaultRetryOptions);
        await context.CallActivityAsync(
            nameof(Brs023CalculationStepTerminateActivityV1),
            context.InstanceId,
            defaultRetryOptions);

        // Terminate

        return "Success";
    }

    private static TaskOptions CreateDefaultRetryOptions()
    {
        return TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(30),
            backoffCoefficient: 2.0));
    }
}
