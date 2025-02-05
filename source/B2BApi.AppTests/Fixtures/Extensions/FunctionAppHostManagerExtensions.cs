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

using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.Core.TestCommon.Diagnostics;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;

public static class FunctionAppHostManagerExtensions
{
    private const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Wait for a function with the given name to complete execution with succeeded.
    /// <remarks>
    /// This is done by checking the host logs for the "Executed 'Functions.{functionName}' (Succeeded" message.
    /// </remarks>
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="functionName">The function name (without the "Functions." prefix).</param>
    /// <param name="timeout">The optional timeout to wait for the function. Defaults to <see cref="DefaultTimeoutSeconds"/> seconds.</param>
    public static async Task<(bool Succeeded, IReadOnlyList<string> HostLog)> WaitForFunctionToCompleteWithSucceededAsync(
        this FunctionAppHostManager manager,
        string functionName,
        TimeSpan? timeout = null)
    {
        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => manager.CheckIfFunctionWasExecuted($"Functions.{functionName}"),
            timeLimit: timeout ?? TimeSpan.FromSeconds(DefaultTimeoutSeconds));
        var hostLog = manager.GetHostLogSnapshot();

        var succeeded = didFinish
            && hostLog.Any(l => l.Contains($"Executed 'Functions.{functionName}' (Succeeded,"));

        return (succeeded, hostLog);
    }
}
