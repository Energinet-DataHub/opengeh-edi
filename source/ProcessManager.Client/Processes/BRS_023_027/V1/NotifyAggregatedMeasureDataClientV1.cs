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
using System.Text.Json;
using Energinet.DataHub.ProcessManager.Api.Model;
using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;

namespace Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1;

/// <inheritdoc/>
internal class NotifyAggregatedMeasureDataClientV1 : INotifyAggregatedMeasureDataClientV1
{
    public NotifyAggregatedMeasureDataClientV1(string baseUrl, HttpClient httpClient)
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
    public async Task<Guid> ScheduleNewCalculationOrchestationInstanceAsync(
        ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1> requestDto,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/processmanager/orchestrationinstance/brs_023_027/v1");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestDto),
            Encoding.UTF8,
            "application/json");

        using var actualResponse = await HttpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        actualResponse.EnsureSuccessStatusCode();

        var calculationId = await actualResponse.Content
            .ReadFromJsonAsync<Guid>(cancellationToken)
            .ConfigureAwait(false);

        return calculationId;
    }

    /// <inheritdoc/>
    public Task<OrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1>> GetCalculationOrchestrationInstanceAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<OrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1>>> SearchCalculationOrchestrationInstancesAsync(
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        IReadOnlyCollection<CalculationTypes>? calculationTypes,
        IReadOnlyCollection<string>? gridAreaCodes,
        DateTimeOffset? startedAtOrLater,
        DateTimeOffset? terminatedAtOrEarlier,
        DateTimeOffset? periodStartDate,
        DateTimeOffset? periodEndDate,
        bool? isInternalCalculation,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
