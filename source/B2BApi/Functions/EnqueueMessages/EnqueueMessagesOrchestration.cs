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

        var defaultRetryOptions = CreateDefaultRetryOptions();
        var enqueueRetryOptions = CreateEnqueueRetryOptions();

        // Fan-out/fan-in => https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-cloud-backup?tabs=csharp
        var tasks = new Task<int>[5];
        tasks[0] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForGridAreaOwnersActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: enqueueRetryOptions);

        tasks[1] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: enqueueRetryOptions);

        tasks[2] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: enqueueRetryOptions);

        tasks[3] = context.CallActivityAsync<int>(
            nameof(EnqueueWholesaleResultsForAmountPerChargesActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: enqueueRetryOptions);

        tasks[4] = context.CallActivityAsync<int>(
            nameof(EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity),
            new EnqueueMessagesInput(input.CalculationId, input.EventId),
            options: enqueueRetryOptions);

        await Task.WhenAll(tasks);

        var resultsWasSuccessfullyHandled = ResultsWasSuccessfullyHandled(tasks.Select(t => t.Result));
        await context.CallActivityAsync(
            nameof(SendActorMessagesEnqueuedActivity),
            new SendMessagesEnqueuedInput(
                context.InstanceId,
                input.CalculationOrchestrationId,
                input.CalculationId,
                resultsWasSuccessfullyHandled),
            defaultRetryOptions);

        return "Success";
    }

    private static TaskOptions CreateEnqueueRetryOptions()
    {
        return TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: int.MaxValue,
            firstRetryInterval: TimeSpan.FromSeconds(2),
            backoffCoefficient: 2.0,
            maxRetryInterval: TimeSpan.FromHours(1)));
    }

    private static TaskOptions CreateDefaultRetryOptions()
    {
        return TaskOptions.FromRetryPolicy(new RetryPolicy(
            maxNumberOfAttempts: 5,
            firstRetryInterval: TimeSpan.FromSeconds(30),
            backoffCoefficient: 2.0));
    }

    private static bool ResultsWasSuccessfullyHandled(IEnumerable<int> numberOfHandledResults)
    {
        return numberOfHandledResults.Sum() > 0;
    }
}
