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

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.B2BApi.DataRetention;

public class RetentionJobTimer(ExecuteDataRetentionJobs executeDataRetentionJobs)
{
    private readonly ExecuteDataRetentionJobs _executeDataRetentionJobs = executeDataRetentionJobs;

    [Function("RetentionJobTimer")]
    public async Task RunAsync([TimerTrigger("0 0 10-23 * * *")] TimerInfo timerTimerInfo, FunctionContext context)
    {
        var cancellationToken = context.InstanceServices.GetRequiredService<CancellationToken>();
        await _executeDataRetentionJobs.CleanupAsync(cancellationToken).ConfigureAwait(false);
    }
}
