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

using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Options;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ProcessManager.Scheduler;

public class ProcessSchedulerHandler(
    IOptions<ProcessManagerOptions> processManagerOptions,
    IDurableClientFactory clientFactory)
{
    private readonly ProcessManagerOptions _processManagerOptions = processManagerOptions.Value;
    private readonly IDurableClientFactory _clientFactory = clientFactory;

    public async Task StartScheduledProcessAsync()
    {
        var x = 12 + 2;
        if (x == 13)
        {
            // TODO: Move to ProcessManager.Core
            var durableClient = _clientFactory.CreateClient(new DurableClientOptions
            {
                ConnectionName = nameof(ProcessManagerOptions.ProcessManagerStorageConnectionString),
                TaskHub = _processManagerOptions.ProcessManagerTaskHubName,
                IsExternalClient = true,
            });

            var orchestrationInstanceId = await durableClient
                .StartNewAsync(
                    "NotifyAggregatedMeasureDataOrchestration")
                .ConfigureAwait(false);
        }
    }
}
