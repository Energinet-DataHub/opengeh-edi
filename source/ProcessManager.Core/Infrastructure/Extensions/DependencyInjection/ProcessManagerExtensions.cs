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
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Options;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.Options;
using Microsoft.EntityFrameworkCore;
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
    /// Register options and services necessary for enabling an application to use the Process Manager
    /// to manage and monitor orchestrations.
    /// Should be used from the Process Manager API / Scheduler application.
    /// </summary>
    public static IServiceCollection AddProcessManagerCore(this IServiceCollection services)
    {
        // Process Manager Core
        services
            .AddProcessManagerOptions()
            .AddProcessManagerDatabase();

        // DurableClient connected to Task Hub
        services.AddTaskHubStorage();
        services
            .AddDurableClientFactory()
            .TryAddSingleton<IDurableClient>(sp =>
            {
                // IDurableClientFactory has a singleton lifecycle and caches clients
                var clientFactory = sp.GetRequiredService<IDurableClientFactory>();
                var processManagerOptions = sp.GetRequiredService<IOptions<ProcessManagerTaskHubOptions>>().Value;

                var durableClient = clientFactory.CreateClient(new DurableClientOptions
                {
                    ConnectionName = nameof(ProcessManagerTaskHubOptions.ProcessManagerStorageConnectionString),
                    TaskHub = processManagerOptions.ProcessManagerTaskHubName,
                    IsExternalClient = true,
                });

                return durableClient;
            });

        // ProcessManager components using interfaces to restrict access to functionality
        services.TryAddScoped<IOrchestrationRegisterQueries, OrchestrationRegister>();
        services.TryAddScoped<IOrchestrationInstanceRepository, OrchestrationInstanceRepository>();
        services.TryAddScoped<IOrchestrationInstanceManager, OrchestrationInstanceManager>();

        return services;
    }

    /// <summary>
    /// Register options and services necessary for integrating Durable Functions orchestrations with the Process Manager functionality.
    /// Should be used from host's that contains Durable Functions orchestrations.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="enabledDescriptionsFactory">
    /// Build descriptions for all Durable Function orchestrations that should be enabled.
    /// Leave out descriptions for any Durable Function orchestrations that should be disabled.
    /// </param>
    public static IServiceCollection AddProcessManagerForOrchestrations(
        this IServiceCollection services,
        Func<IReadOnlyCollection<OrchestrationDescription>> enabledDescriptionsFactory)
    {
        ArgumentNullException.ThrowIfNull(enabledDescriptionsFactory);

        // Process Manager Core
        services
            .AddProcessManagerOptions()
            .AddProcessManagerDatabase();

        // Task Hub connected to Durable Functions
        services.AddTaskHubStorage();

        // Orchestration Descriptions to register
        services.TryAddTransient<IReadOnlyCollection<OrchestrationDescription>>(sp => enabledDescriptionsFactory());

        // ProcessManager components using interfaces to restrict access to functionality
        services.TryAddTransient<IOrchestrationRegister, OrchestrationRegister>();
        services.TryAddScoped<IOrchestrationInstanceProgressRepository, OrchestrationInstanceRepository>();

        return services;
    }

    /// <summary>
    /// Register hierarchical Process Manager options.
    /// </summary>
    private static IServiceCollection AddProcessManagerOptions(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerOptions>()
            .BindConfiguration(ProcessManagerOptions.SectionName)
            .ValidateDataAnnotations();

        return services;
    }

    /// <summary>
    /// Register Process Manager database and health checks.
    /// Depends on <see cref="ProcessManagerOptions"/>.
    /// </summary>
    private static IServiceCollection AddProcessManagerDatabase(this IServiceCollection services)
    {
        services
            .AddDbContext<ProcessManagerContext>((sp, optionsBuilder) =>
            {
                var processManagerOptions = sp.GetRequiredService<IOptions<ProcessManagerOptions>>().Value;

                optionsBuilder.UseSqlServer(processManagerOptions.SqlDatabaseConnectionString, providerOptionsBuilder =>
                {
                    providerOptionsBuilder.UseNodaTime();
                    providerOptionsBuilder.EnableRetryOnFailure();
                });
            })
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddHealthChecks()
            .AddDbContextCheck<ProcessManagerContext>(name: "ProcesManagerDatabase");

        return services;
    }

    /// <summary>
    /// Register Task Hub storage options and health checks.
    /// </summary>
    private static IServiceCollection AddTaskHubStorage(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerTaskHubOptions>()
            .BindConfiguration(configSectionPath: string.Empty)
            .ValidateDataAnnotations();

        services
            .AddHealthChecks();
        // TODO: Add health check against Task Hub which means: Blob Containers, Queues and Tables

        return services;
    }
}
