﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Diagnostics;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;

namespace Energinet.DataHub.EDI.IntegrationTests.DataRetention;

public class SleepyDataRetentionJob(int executionTimeLimitInSeconds = 3) : IDataRetention
{
    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (stopwatch.Elapsed.TotalSeconds < executionTimeLimitInSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Delay for 1 second
        }

        stopwatch.Stop();
    }
}
