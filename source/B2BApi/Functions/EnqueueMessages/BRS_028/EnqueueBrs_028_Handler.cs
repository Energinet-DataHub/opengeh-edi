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

using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_028.V1.Model;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;

public class EnqueueBrs_028_Handler(ILogger<EnqueueBrs_028_Handler> logger)
    : EnqueueValidatedMessagesHandlerBase<RequestCalculatedWholesaleServicesInputV1, RequestCalculatedWholesaleServicesRejectedV1>(logger)
{
    private readonly ILogger _logger = logger;

    protected override async Task EnqueueAcceptedMessagesAsync(RequestCalculatedWholesaleServicesInputV1 acceptedMessagesData)
    {
        _logger.LogInformation(
            "Received enqueue accepted message(s) for BRS 028. Data: {0}",
            acceptedMessagesData);

        // TODO: Call actual logic that enqueues accepted messages instead
        await Task.CompletedTask.ConfigureAwait(false);
    }

    protected override async Task EnqueueRejectedMessagesAsync(RequestCalculatedWholesaleServicesRejectedV1 rejectedMessagesData)
    {
        _logger.LogInformation(
            "Received enqueue rejected message(s) for BRS 028. Data: {0}",
            rejectedMessagesData);

        // TODO: Call actual logic that enqueues rejected message
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
