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
using Energinet.DataHub.EDI.B2BApi.MetricTracker;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Model;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

internal class EnqueueMessagesOrchestration(TelemetryClient telemetryClient)
{
    [Function(nameof(EnqueueMessagesOrchestration))]
    public async Task<string> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        FunctionContext executionContext)
    {
        // Record the start time using the deterministic context.CurrentUtcDateTime
        var startTime = context.CurrentUtcDateTime;

        var input = context.GetInput<EnqueueMessagesOrchestrationInput>();
        if (input == null)
        {
            return "Error: No input specified.";
        }

        var defaultRetryOptions = CreateDefaultRetryOptions();
        var enqueueRetryOptions = CreateEnqueueRetryOptions();

        var enqueueMessagesInput = new EnqueueMessagesInput(input.CalculationId, input.EventId);

        // Fan-out/fan-in => https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-cloud-backup?tabs=csharp
        var tasks = new Task<int>[6];
        tasks[0] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForGridAreaOwnersActivity),
            enqueueMessagesInput,
            options: enqueueRetryOptions);

        tasks[1] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesActivity),
            enqueueMessagesInput,
            options: enqueueRetryOptions);

        tasks[2] = context.CallActivityAsync<int>(
            nameof(EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity),
            enqueueMessagesInput,
            options: enqueueRetryOptions);

        tasks[3] = EnqueueWholesaleResultsForAmountPerChargesAsync(context, enqueueMessagesInput, enqueueRetryOptions);

        tasks[4] = EnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(context, enqueueMessagesInput, enqueueRetryOptions);

        tasks[5] = EnqueueWholesaleResultsForTotalAmountPerChargesAsync(context, enqueueMessagesInput, enqueueRetryOptions);

        await Task.WhenAll(tasks);

        var sumOfHandledResults = tasks.Select(t => t.Result).Sum();
        var resultsWasSuccessfullyHandled = ResultsWasSuccessfullyHandled(sumOfHandledResults);
        await context.CallActivityAsync(
            nameof(SendActorMessagesEnqueuedActivity),
            new SendMessagesEnqueuedInput(
                context.InstanceId,
                input.CalculationOrchestrationId,
                input.CalculationId,
                resultsWasSuccessfullyHandled),
            defaultRetryOptions);

        // Record the end time using the deterministic context.CurrentUtcDateTime
        var endTime = context.CurrentUtcDateTime;

        // Calculate the duration
        var duration = endTime - startTime;
        var avgDurationOfHandledResults = duration.Nanoseconds / sumOfHandledResults;
        telemetryClient.GetMetric(CustomMetricConstants.EnqueueMessageAvgDuration).TrackValue(avgDurationOfHandledResults);
        return "Success";
    }

    private static async Task<int> EnqueueWholesaleResultsForAmountPerChargesAsync(TaskOrchestrationContext context, EnqueueMessagesInput input, TaskOptions options)
    {
        var actors = await GetActorsForWholesaleResultsForAmountPerChargesActivity.StartActivityAsync(
            input,
            context,
            options);

        var tasks = new Task<int>[actors.Count];
        for (var i = 0; i < actors.Count; i++)
        {
            var actor = actors.ElementAt(i);
            tasks[i] = EnqueueWholesaleResultsForAmountPerChargesActivity.StartActivityAsync(
                new EnqueueMessagesForActorInput(input.CalculationId, input.EventId, actor),
                context,
                options);
        }

        await Task.WhenAll(tasks);

        var messagesEnqueued = tasks.Sum(t => t.Result);

        return messagesEnqueued;
    }

    private static async Task<int> EnqueueWholesaleResultsForMonthlyAmountPerChargesAsync(TaskOrchestrationContext context, EnqueueMessagesInput input, TaskOptions options)
    {
        var actors = await GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity.StartActivityAsync(
            input,
            context,
            options);

        var tasks = new Task<int>[actors.Count];
        for (var i = 0; i < actors.Count; i++)
        {
            var actor = actors.ElementAt(i);
            tasks[i] = EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity.StartActivityAsync(
                new EnqueueMessagesForActorInput(input.CalculationId, input.EventId, actor),
                context,
                options);
        }

        await Task.WhenAll(tasks);

        var messagesEnqueued = tasks.Sum(t => t.Result);

        return messagesEnqueued;
    }

    private static async Task<int> EnqueueWholesaleResultsForTotalAmountPerChargesAsync(TaskOrchestrationContext context, EnqueueMessagesInput input, TaskOptions options)
    {
        var actors = await GetActorsForWholesaleResultsForTotalAmountPerChargesActivity.StartActivityAsync(
            input,
            context,
            options);

        var tasks = new Task<int>[actors.Count];
        for (var i = 0; i < actors.Count; i++)
        {
            var actor = actors.ElementAt(i);
            tasks[i] = EnqueueWholesaleResultsForTotalAmountsActivity.StartActivityAsync(
                new EnqueueMessagesForActorInput(input.CalculationId, input.EventId, actor),
                context,
                options);
        }

        await Task.WhenAll(tasks);

        var messagesEnqueued = tasks.Sum(t => t.Result);

        return messagesEnqueued;
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

    private static bool ResultsWasSuccessfullyHandled(int sumOfHandledResults)
    {
        return sumOfHandledResults > 0;
    }
}
