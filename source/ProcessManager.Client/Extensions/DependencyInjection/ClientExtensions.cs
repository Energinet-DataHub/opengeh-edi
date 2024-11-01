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

using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// that allow adding Process Manager clients to an application.
/// </summary>
public static class ClientExtensions
{
    // TODO: Add description
    public static IServiceCollection AddProcessManagerClients(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerClientOptions>()
            .BindConfiguration(ProcessManagerClientOptions.SectionName)
            .ValidateDataAnnotations();

        // TODO: Consider using `AddHttpClient` => https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddScoped<IProcessManagerClient, ProcessManagerClient>();
        // TODO: Consider using `AddHttpClient` => https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddScoped<INotifyAggregatedMeasureDataClientV1, NotifyAggregatedMeasureDataClientV1>();

        // TODO: Do we want to automatically add "live" health check against Process Manager API? I think not, but lets talk.
        return services;
    }
}
