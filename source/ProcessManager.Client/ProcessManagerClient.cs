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

using System.Net.Http.Json;
using System.Text;
using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

namespace Energinet.DataHub.ProcessManager.Client;

/// <inheritdoc/>
internal class ProcessManagerClient : IProcessManagerClient
{
    public ProcessManagerClient(string baseUrl, HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(httpClient);

        BaseUrl = baseUrl;
        HttpClient = httpClient;

        HttpClient.BaseAddress = new Uri(BaseUrl);
    }

    public string BaseUrl { get; }

    protected HttpClient HttpClient { get; }

    /// <inheritdoc/>
    public async Task CancelScheduledOrchestrationInstanceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"api/processmanager/orchestrationinstance/{id}");

        using var actualResponse = await HttpClient
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
            $"api/processmanager/orchestrationinstance/{id}");

        using var actualResponse = await HttpClient
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
        CancellationToken cancellationToken)
    {
        var url = BuildRequestUrl(name, version, lifecycleState, terminationState);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        using var actualResponse = await HttpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        actualResponse.EnsureSuccessStatusCode();

        var orchestrationInstances = await actualResponse.Content
            .ReadFromJsonAsync<IReadOnlyCollection<OrchestrationInstanceDto>>(cancellationToken)
            .ConfigureAwait(false);

        return orchestrationInstances!;
    }

    private static string BuildRequestUrl(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState)
    {
        var urlBuilder = new StringBuilder($"processmanager/orchestrationinstances/{name}");

        if (version.HasValue)
            urlBuilder.Append($"/{version}");

        if (lifecycleState.HasValue || terminationState.HasValue)
        {
            urlBuilder.Append("?");

            if (lifecycleState.HasValue)
                urlBuilder.Append($"lifecycleState={lifecycleState.ToString()}&");

            if (terminationState.HasValue)
                urlBuilder.Append($"terminationState={terminationState.ToString()}&");
        }

        return urlBuilder.ToString();
    }
}
