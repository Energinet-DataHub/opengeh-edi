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

using System.Text;
using System.Text.Json;
using Energinet.DataHub.EDI.SubsystemTests.Logging;
using Energinet.DataHub.ProcessManager.Components.Abstractions.EnqueueActorMessages;
using Nito.AsyncEx;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

/// <summary>
/// The role of this driver is to act as an arbitrary subsystem.
/// That is, for any interaction EDI has, which may come from more than one subsystem.
/// Then that interaction should be started via this driver.
/// </summary>
public class SubsystemDriver
{
    private readonly AsyncLazy<HttpClient> _subsystemHttpClient;
    private readonly ITestOutputHelper _logger;

    public SubsystemDriver(
        AsyncLazy<HttpClient> subsystemHttpClient,
        ITestOutputHelper logger)
    {
        _subsystemHttpClient = subsystemHttpClient;
        _logger = logger;
    }

    internal async Task EnqueueActorMessagesViaHttpAsync(IEnqueueDataSyncDto data)
    {
        var json = JsonSerializer.Serialize(data, data.GetType());
        var httpClient = await _subsystemHttpClient;

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/enqueue/" + data.Route);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        await response.EnsureSuccessStatusCodeWithLogAsync(_logger);
    }
}
