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

using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages;

public abstract class EnqueueMessagesTestBase : IAsyncLifetime
{
    // This string must match the subject defined in the "ProcessManagerMessageClient" from the process manager
    private const string NotifyOrchestrationInstanceSubject = "NotifyOrchestration";
    private readonly B2BApiAppFixture _fixture;

    public EnqueueMessagesTestBase(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public abstract Task InitializeAsync();

    public abstract Task DisposeAsync();

    internal async Task<bool> ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
        Guid orchestrationInstanceId,
        string eventName)
    {
        var verifyServiceBusMessages = await _fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != NotifyOrchestrationInstanceSubject)
                    return false;

                var parsedNotification = NotifyOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var matchingOrchestrationId = parsedNotification.OrchestrationInstanceId == orchestrationInstanceId.ToString();
                var matchingEvent = parsedNotification.EventName == eventName;

                return matchingOrchestrationId && matchingEvent;
            })
            .VerifyCountAsync(1);

        var wasSent = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        return wasSent;
    }
}
