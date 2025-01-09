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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Energinet.DataHub.EDI.B2BApi.DataRetention;

public class ExecuteDataRetentionJobs
{
    private readonly IReadOnlyCollection<IDataRetention> _dataRetentions;
    private readonly ILogger<ExecuteDataRetentionJobs> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly int _jobsExecutionTimeLimitInSeconds;

    public ExecuteDataRetentionJobs(
        IEnumerable<IDataRetention> dataRetentions,
        ILogger<ExecuteDataRetentionJobs> logger,
        IServiceScopeFactory serviceScopeFactory,
        int executionTimeLimitInSeconds = 25 * 60)
    {
        _dataRetentions = dataRetentions.ToList();
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _jobsExecutionTimeLimitInSeconds = executionTimeLimitInSeconds;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var taskMap = new Dictionary<Task, IDataRetention>();
        List<IServiceScope> serviceScopes = [];
        try
        {
            // Cancels all retentions after provided seconds
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_jobsExecutionTimeLimitInSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var executionPolicy = Policy
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .WaitAndRetryAsync(
                    [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)]);

            foreach (var dataCleaner in _dataRetentions)
            {
                // Cannot dispose the scope in the foreach loop, as the task may still be running
                var scope = _serviceScopeFactory.CreateScope();
                var scopedRetentionJob = scope.ServiceProvider.GetServices<IDataRetention>()
                    .Single(j => j.GetType() == dataCleaner.GetType());

                var task = executionPolicy.ExecuteAsync(
                    () => scopedRetentionJob.CleanupAsync(linkedCts.Token));

                taskMap[task] = dataCleaner;
                serviceScopes.Add(scope);
            }

            var tasks = taskMap.Keys.ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            // This catch block handles task-specific cancellations.
            // It logs the cancellation of data retention jobs.
            LogCancelledTasks(taskMap, ex);
        }
        finally
        {
            foreach (var scope in serviceScopes)
            {
                scope.Dispose();
            }
        }
    }

    private void LogCancelledTasks(Dictionary<Task, IDataRetention> taskMap, TaskCanceledException ex)
    {
        var incompleteTasks = taskMap
            .Where(kvp => kvp.Key.Status != TaskStatus.RanToCompletion)
            .Select(kvp => kvp.Value);
        foreach (var dataCleaner in incompleteTasks)
        {
            _logger?.LogError(
                ex,
                "Data retention job {DataCleaner} was cancelled.",
                dataCleaner.GetType().FullName);
        }
    }
}
