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

using Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices.Activities;
using Energinet.DataHub.EDI.Process.Interfaces;
using Energinet.DataHub.Wholesale.Edi.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices;

public class RequestWholesaleServicesOrchestration
{
    [Function(nameof(RequestWholesaleServicesOrchestration))]
    public async Task<string> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        FunctionContext executionContext)
    {
        var input = context.GetInput<InitializeWholesaleServicesProcessDto>();
        if (input == null)
            return "Error: No input given.";

        // Split request in multiple transactions
        var transactions = await GetWholesaleServicesRequestTransactionsActivity
            .StartActivityAsync(
                input,
                context,
                null);

        // TODO: Maybe use sub orchestrations instead of fan-out?
        var transactionTasks = new Task<EnqueueMessagesForRequestWholesaleServicesActivity.EnqueueMessagesResult>[transactions.Count];
        for (var i = 0; i < transactions.Count; i++)
        {
            transactionTasks[i] = HandleRequestWholesaleServicesTransaction(
                transactions.ElementAt(i),
                context);
        }

        var enqueueMessagesResults = await Task.WhenAll(transactionTasks);

        return "Enqueueing finished. " +
               $"AcceptedMessagesCount={enqueueMessagesResults.Sum(r => r.AcceptedMessagesCount)}, " +
               $"RejectedMessagesCount={enqueueMessagesResults.Sum(r => r.RejectedMessagesCount)}";
    }

    private async Task<EnqueueMessagesForRequestWholesaleServicesActivity.EnqueueMessagesResult> HandleRequestWholesaleServicesTransaction(
        RequestWholesaleServicesTransaction transaction,
        TaskOrchestrationContext context)
    {
        // Perform "Wholesale" part from WholesaleServicesRequestHandler
        // 1. Validate request
        //      1b) Enqueue reject message (and terminate) if validation fails
        var validationErrors = await ValidateWholesaleServicesRequestActivity.StartActivityAsync(
            transaction,
            context,
            null);

        if (validationErrors.Any()) // TODO: Enqueue reject message
            throw new Exception("Validation failed: " + string.Join("; ", validationErrors.Select(v => v.Message)));

        // 2. Perform wholesale services query and enqueue messages
        //      2b) Enqueue reject message (and terminate) if no data
        //      2c) Enqueue accept message if data exists
        return await EnqueueMessagesForRequestWholesaleServicesActivity.StartActivityAsync(
            transaction,
            context,
            null);
    }
}
