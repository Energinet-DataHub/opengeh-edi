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
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_026.V1.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_026.V1;

// TODO: Implement according to guidelines: https://energinet.atlassian.net/wiki/spaces/D3/pages/824803345/Durable+Functions+Development+Guidelines
internal class RequestCalculatedEnergyTimeSeriesOrchestrationV1
{
    [Function(nameof(RequestCalculatedEnergyTimeSeriesOrchestrationV1))]
    public async Task<string> Run(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetOrchestrationParameterValue<RequestCalculatedEnergyTimeSeriesInputV1>();

        if (input == null)
            return "Error: No input specified.";

        await Task.CompletedTask;

        /*
         * Steps:
         * 1. Deserialize input
         * 2. Async validation
         * 3. Query databricks and upload to storage account
         * 4. Enqueue Messages in EDI
         * 5. Wait for notify from EDI
         * 6. Complete process in database
         */

        return $"Success (BusinessReason={input.BusinessReason})";
    }
}
