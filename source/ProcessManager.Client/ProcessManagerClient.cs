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

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.ProcessManager.Api.Model;
using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;

namespace Energinet.DataHub.ProcessManager.Client;

/// <inheritdoc/>
internal class ProcessManagerClient : IProcessManagerClient
{
    private readonly HttpClient _httpClient;

    public ProcessManagerClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.GeneralApi);
    }

    /// <inheritdoc/>
    public async Task CancelScheduledOrchestrationInstanceAsync(
        CancelScheduledOrchestrationInstanceCommand command,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/processmanager/orchestrationinstance/cancel");
        request.Content = new StringContent(
            JsonSerializer.Serialize(command),
            Encoding.UTF8,
            "application/json");

        using var actualResponse = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        actualResponse.EnsureSuccessStatusCode();
    }

    /// <inheritdoc/>
    public async Task<OrchestrationInstanceDto> GetOrchestrationInstanceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/processmanager/orchestrationinstance/{id}");

        using var actualResponse = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        actualResponse.EnsureSuccessStatusCode();

        var orchestrationInstance = await actualResponse.Content
            .ReadFromJsonAsync<OrchestrationInstanceDto>(cancellationToken)
            .ConfigureAwait(false);

        return orchestrationInstance!;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrchestrationInstanceDto>> SearchOrchestrationInstancesAsync(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        DateTimeOffset? startedAtOrLater,
        DateTimeOffset? terminatedAtOrEarlier,
        CancellationToken cancellationToken)
    {
        var url = BuildSearchRequestUrl(name, version, lifecycleState, terminationState, startedAtOrLater, terminatedAtOrEarlier);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        using var actualResponse = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        actualResponse.EnsureSuccessStatusCode();

        var orchestrationInstances = await actualResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<OrchestrationInstanceDto>>(cancellationToken)
            .ConfigureAwait(false);

        return orchestrationInstances!;
    }

    // TODO: Perhaps share with other clients
    private static string BuildSearchRequestUrl(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        DateTimeOffset? startedAtOrLater,
        DateTimeOffset? terminatedAtOrEarlier)
    {
        var urlBuilder = new StringBuilder($"/api/processmanager/orchestrationinstances/{name}");

        if (version.HasValue)
            urlBuilder.Append($"/{version}");

        urlBuilder.Append("?");

        if (lifecycleState.HasValue)
            urlBuilder.Append($"lifecycleState={Uri.EscapeDataString(lifecycleState.ToString() ?? string.Empty)}&");

        if (terminationState.HasValue)
            urlBuilder.Append($"terminationState={Uri.EscapeDataString(terminationState.ToString() ?? string.Empty)}&");

        if (startedAtOrLater.HasValue)
            urlBuilder.Append($"startedAtOrLater={Uri.EscapeDataString(startedAtOrLater?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty)}&");

        if (terminatedAtOrEarlier.HasValue)
            urlBuilder.Append($"terminatedAtOrEarlier={Uri.EscapeDataString(terminatedAtOrEarlier?.ToString("o", CultureInfo.InvariantCulture) ?? string.Empty)}&");

        return urlBuilder.ToString();
    }
}
