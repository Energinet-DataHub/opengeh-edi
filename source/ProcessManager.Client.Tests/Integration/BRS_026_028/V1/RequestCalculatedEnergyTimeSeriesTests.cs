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

extern alias ClientTypes;

using Azure.Messaging.ServiceBus;
using ClientTypes::Energinet.DataHub.ProcessManager.Api.Model;
using ClientTypes::Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;
using ClientTypes::Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using ClientTypes::Energinet.DataHub.ProcessManager.Client.Processes.BRS_026_028.V1;
using ClientTypes::Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_026.V1.Models;
using Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
using Microsoft.Extensions.Azure;
using Moq;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Integration.BRS_026_028.V1;

/// <summary>
/// Test collection that verifies the Process Manager clients can be used to start a
/// request calculated energy time series orchestration and monitor its status during its lifetime.
/// </summary>
[Collection(nameof(ProcessManagerClientCollection))]
public class RequestCalculatedEnergyTimeSeriesTests : IAsyncLifetime
{
    public RequestCalculatedEnergyTimeSeriesTests(
        ProcessManagerClientFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    public ProcessManagerClientFixture Fixture { get; }

    public Task InitializeAsync()
    {
        Fixture.ProcessManagerAppManager.AppHostManager.ClearHostLog();
        Fixture.OrchestrationsAppManager.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestCalculatedEnergyTimeSeries_WhenStartedUsingClient_CanMonitorLifecycle()
    {
        // Arrange
        var serviceBusSenderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        serviceBusSenderFactoryMock.Setup(
                f =>
                    f.CreateClient(nameof(ProcessManagerServiceBusClientsOptions.TopicName)))
            .Returns(Fixture.ProcessManagerTopic.SenderClient);

        var requestCalculatedDataClient = new RequestCalculatedDataClientV1(serviceBusSenderFactoryMock.Object);
        var input = new MessageCommand<RequestCalculatedEnergyTimeSeriesInputV1>(
            new ActorIdentityDto(Guid.NewGuid()),
            new RequestCalculatedEnergyTimeSeriesInputV1("B1337"),
            "servicebus-message-id");

        // Act
        await requestCalculatedDataClient.RequestCalculatedEnergyTimeSeriesAsync(input, CancellationToken.None);

        // Assert
        // TODO: Get orchestration instance status from PM Api based on message id
        await Task.CompletedTask;
    }
}
