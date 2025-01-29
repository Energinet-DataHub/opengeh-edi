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

using DurableTask.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;

public sealed class DurableClientStub : IDurableClient
{
    private readonly string _taskHubName = "taskHubName1";
    private readonly string _taskHubName1 = "taskHubName2";
    private readonly string _taskHubName2 = "taskHubName3";

    string IDurableClient.TaskHubName => _taskHubName2;

    string IDurableEntityClient.TaskHubName => _taskHubName1;

    string IDurableOrchestrationClient.TaskHubName => _taskHubName;

    public int NumberOfJobsStarted { get;  } = 0;

    public HttpResponseMessage CreateCheckStatusResponse(
        HttpRequestMessage request,
        string instanceId,
        bool returnInternalServerErrorOnFailure = false)
    {
        throw new NotImplementedException();
    }

    public IActionResult CreateCheckStatusResponse(
        HttpRequest request,
        string instanceId,
        bool returnInternalServerErrorOnFailure = false)
    {
        throw new NotImplementedException();
    }

    public HttpManagementPayload CreateHttpManagementPayload(string instanceId)
    {
        throw new NotImplementedException();
    }

    public Task<HttpResponseMessage> WaitForCompletionOrCreateCheckStatusResponseAsync(
        HttpRequestMessage request,
        string instanceId,
        TimeSpan? timeout = null,
        TimeSpan? retryInterval = null,
        bool returnInternalServerErrorOnFailure = false)
    {
        throw new NotImplementedException();
    }

    public Task<IActionResult> WaitForCompletionOrCreateCheckStatusResponseAsync(
        HttpRequest request,
        string instanceId,
        TimeSpan? timeout = null,
        TimeSpan? retryInterval = null,
        bool returnInternalServerErrorOnFailure = false)
    {
        throw new NotImplementedException();
    }

    public Task<string> StartNewAsync(string orchestratorFunctionName, string? instanceId = null)
    {
        NumberOfJobsStarted++;
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task<string> StartNewAsync<T>(string orchestratorFunctionName, T input)
        where T : class
    {
        NumberOfJobsStarted++;
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task<string> StartNewAsync<T>(string orchestratorFunctionName, string instanceId, T input)
    {
        NumberOfJobsStarted++;
        return Task.FromResult(Guid.NewGuid().ToString());
    }

    public Task RaiseEventAsync(string instanceId, string eventName, object? eventData = null)
    {
        throw new NotImplementedException();
    }

    public Task RaiseEventAsync(
        string taskHubName,
        string instanceId,
        string eventName,
        object eventData,
        string? connectionName = null)
    {
        throw new NotImplementedException();
    }

    public Task TerminateAsync(string instanceId, string reason)
    {
        throw new NotImplementedException();
    }

    public Task SuspendAsync(string instanceId, string reason)
    {
        throw new NotImplementedException();
    }

    public Task ResumeAsync(string instanceId, string reason)
    {
        throw new NotImplementedException();
    }

    public Task RewindAsync(string instanceId, string reason)
    {
        throw new NotImplementedException();
    }

    public Task<DurableOrchestrationStatus> GetStatusAsync(string instanceId, bool showHistory = false, bool showHistoryOutput = false, bool showInput = true)
    {
        throw new NotImplementedException();
    }

    public Task<IList<DurableOrchestrationStatus>> GetStatusAsync(
        IEnumerable<string> instanceIds,
        bool showHistory = false,
        bool showHistoryOutput = false,
        bool showInput = false)
    {
        throw new NotImplementedException();
    }

    public Task<IList<DurableOrchestrationStatus>> GetStatusAsync(
        DateTime? createdTimeFrom = null,
        DateTime? createdTimeTo = null,
        IEnumerable<OrchestrationRuntimeStatus>? runtimeStatus = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PurgeHistoryResult> PurgeInstanceHistoryAsync(string instanceId)
    {
        throw new NotImplementedException();
    }

    public Task<PurgeHistoryResult> PurgeInstanceHistoryAsync(IEnumerable<string> instanceIds)
    {
        throw new NotImplementedException();
    }

    public Task<PurgeHistoryResult> PurgeInstanceHistoryAsync(DateTime createdTimeFrom, DateTime? createdTimeTo, IEnumerable<OrchestrationStatus> runtimeStatus)
    {
        throw new NotImplementedException();
    }

    public Task<OrchestrationStatusQueryResult> GetStatusAsync(OrchestrationStatusQueryCondition condition, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<OrchestrationStatusQueryResult> ListInstancesAsync(OrchestrationStatusQueryCondition condition, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<string> RestartAsync(string instanceId, bool restartWithNewInstanceId = true)
    {
        throw new NotImplementedException();
    }

    public Task MakeCurrentAppPrimaryAsync()
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync(
        EntityId entityId,
        string operationName,
        object? operationInput = null,
        string? taskHubName = null,
        string? connectionName = null)
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync(
        EntityId entityId,
        DateTime scheduledTimeUtc,
        string operationName,
        object? operationInput = null,
        string? taskHubName = null,
        string? connectionName = null)
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync<TEntityInterface>(string entityKey, Action<TEntityInterface> operation)
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync<TEntityInterface>(string entityKey, DateTime scheduledTimeUtc, Action<TEntityInterface> operation)
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync<TEntityInterface>(EntityId entityId, Action<TEntityInterface> operation)
    {
        throw new NotImplementedException();
    }

    public Task SignalEntityAsync<TEntityInterface>(EntityId entityId, DateTime scheduledTimeUtc, Action<TEntityInterface> operation)
    {
        throw new NotImplementedException();
    }

    public Task<EntityStateResponse<T>> ReadEntityStateAsync<T>(EntityId entityId, string? taskHubName = null, string? connectionName = null)
    {
        throw new NotImplementedException();
    }

    public Task<EntityQueryResult> ListEntitiesAsync(EntityQuery query, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<CleanEntityStorageResult> CleanEntityStorageAsync(bool removeEmptyEntities, bool releaseOrphanedLocks, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
