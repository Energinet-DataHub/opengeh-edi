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

using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Options;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.DependencyInjection;

public static class ProcessManagerExtensions
{
    public static IServiceCollection AddOrchestrationManager(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerOptions>()
            .BindConfiguration(configSectionPath: string.Empty)
            .ValidateDataAnnotations();

        services.AddDurableClientFactory();
        services.TryAddSingleton<IDurableClient>(sp =>
        {
            var clientFactory = sp.GetRequiredService<IDurableClientFactory>();
            var processManagerOptions = sp.GetRequiredService<IOptions<ProcessManagerOptions>>().Value;

            var durableClient = clientFactory.CreateClient(new DurableClientOptions
            {
                ConnectionName = nameof(ProcessManagerOptions.ProcessManagerStorageConnectionString),
                TaskHub = processManagerOptions.ProcessManagerTaskHubName,
                IsExternalClient = true,
            });

            return durableClient;
        });
        services.TryAddSingleton<OrchestrationManager>();

        return services;
    }
}
