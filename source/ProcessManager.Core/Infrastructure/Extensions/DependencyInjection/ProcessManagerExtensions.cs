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
using Energinet.DataHub.ProcessManagement.Core.Domain;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Options;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.OrchestrationsRegistration;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// that allow adding ProcessManager services to an application.
/// </summary>
public static class ProcessManagerExtensions
{
    /// <summary>
    /// Register options and services for enabling an application to use the <see cref="OrchestrationManager"/>
    /// and <see cref="IOrchestrationRegisterQueries"/>.
    /// </summary>
    public static IServiceCollection AddOrchestrationManager(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerOptions>()
            .BindConfiguration(configSectionPath: string.Empty)
            .ValidateDataAnnotations();

        services.AddDurableClientFactory();
        services.TryAddSingleton<IDurableClient>(sp =>
        {
            // IDurableClientFactory has a singleton lifecycle and caches clients
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

        services.TryAddScoped<IOrchestrationRegisterQueries, OrchestrationRegister>();
        services.TryAddScoped<IOrchestrationManager, OrchestrationManager>();

        return services;
    }

    /// <summary>
    /// Register options and services for enabling an application to register Durable Functions Orchestrations
    /// that can later be e.g. started by using the <see cref="OrchestrationManager"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="enabledDescriptionsFactory">
    /// Build descriptions for all Durable Function orchestrations that should be enabled.
    /// Leave out descriptions for any Durable Function orchestrations that should be disabled.
    /// </param>
    public static IServiceCollection AddOrchestrationRegister(
        this IServiceCollection services,
        Func<IReadOnlyCollection<OrchestrationDescription>> enabledDescriptionsFactory)
    {
        ArgumentNullException.ThrowIfNull(enabledDescriptionsFactory);

        // TODO: We want to enforce application settings are configured as expected. We need to use the options somewhere to do that!
        services
            .AddOptions<ProcessManagerOptions>()
            .BindConfiguration(configSectionPath: string.Empty)
            .ValidateDataAnnotations();

        // TODO: Not sure what we want the lifetime to be for the following types; they are only used once during startup
        services.TryAddTransient<IReadOnlyCollection<OrchestrationDescription>>(sp => enabledDescriptionsFactory());
        services.TryAddTransient<IOrchestrationRegister, OrchestrationRegister>();

        return services;
    }
}
