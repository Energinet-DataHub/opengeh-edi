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
using System.Text;
using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

namespace Energinet.DataHub.ProcessManager.Client;

internal abstract class ProcessManagerClientBase
{
    protected ProcessManagerClientBase(string baseUrl, HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentNullException.ThrowIfNull(httpClient);

        BaseUrl = baseUrl;
        HttpClient = httpClient;

        HttpClient.BaseAddress = new Uri(BaseUrl);
    }

    public string BaseUrl { get; }

    protected HttpClient HttpClient { get; }

    protected static string BuildSearchRequestUrl(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        DateTimeOffset? startedAtOrLater,
        DateTimeOffset? terminatedAtOrEarlier)
    {
        var urlBuilder = new StringBuilder($"processmanager/orchestrationinstances/{name}");

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
