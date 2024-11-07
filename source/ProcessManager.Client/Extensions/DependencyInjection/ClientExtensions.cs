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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>
/// that allow adding Process Manager clients to an application.
/// </summary>
public static class ClientExtensions
{
    /// <summary>
    /// Register Process Manager clients for use in applications.
    /// If <see cref="IHttpContextAccessor"/> is registered we try to retrieve the "Authorization"
    /// header value and forward it to the Process Manager API for authentication/authorization.
    /// </summary>
    public static IServiceCollection AddProcessManagerClients(this IServiceCollection services)
    {
        services
            .AddOptions<ProcessManagerClientOptions>()
            .BindConfiguration(ProcessManagerClientOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddHttpClient(HttpClientNames.GeneralApi, (sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<ProcessManagerClientOptions>>().Value;
            ConfigureHttpClient(sp, httpClient, options.GeneralApiBaseAddress);
        });
        services.AddHttpClient(HttpClientNames.OrchestrationsApi, (sp, httpClient) =>
        {
            var options = sp.GetRequiredService<IOptions<ProcessManagerClientOptions>>().Value;
            ConfigureHttpClient(sp, httpClient, options.OrchestrationsApiBaseAddress);
        });

        services.AddScoped<IProcessManagerClient, ProcessManagerClient>();
        services.AddScoped<INotifyAggregatedMeasureDataClientV1, NotifyAggregatedMeasureDataClientV1>();

        return services;
    }

    /// <summary>
    /// Configure http client base address; and if available then apply
    /// the authorization header from the current HTTP context.
    /// </summary>
    private static void ConfigureHttpClient(IServiceProvider sp, HttpClient httpClient, string baseAddress)
    {
        httpClient.BaseAddress = new Uri(baseAddress);

        var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
        var authorizationHeaderValue = (string?)httpContextAccessor?.HttpContext.Request.Headers["Authorization"];
        if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
            httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeaderValue);
    }
}
