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

using Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
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
        ProcessManagerClientFixture processManagerClientFixture,
        ITestOutputHelper testOutputHelper)
    {
        ProcessManagerClientFixture = processManagerClientFixture;
        ProcessManagerClientFixture.SetTestOutputHelper(testOutputHelper);
    }

    public ProcessManagerClientFixture ProcessManagerClientFixture { get; }

    public Task InitializeAsync()
    {
        ProcessManagerClientFixture.ProcessManagerAppManager.AppHostManager.ClearHostLog();
        ProcessManagerClientFixture.OrchestrationsAppManager.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        ProcessManagerClientFixture.SetTestOutputHelper(null);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestCalculatedEnergyTimeSeries_WhenStartedUsingClient_CanMonitorLifecycle()
    {
        // TODO: Implement test after implementation of shared Service Bus topic in app fixtures
        // Arrange
        // await using var serviceBusClient = new ServiceBusClient(
        //     ProcessManagerClientFixture.IntegrationTestConfiguration.ServiceBusFullyQualifiedNamespace,
        //     ProcessManagerClientFixture.IntegrationTestConfiguration.Credential);
        // var serviceBusSender = serviceBusClient.CreateSender(ProcessManagerClientFixture.Brs026Subscription.TopicName);
        // var serviceBusSenderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        // serviceBusSenderFactoryMock.Setup(
        //         f =>
        //             f.CreateClient(nameof(ProcessManagerServiceBusClientsOptions.TopicName)))
        //     .Returns(serviceBusSender);
        //
        //
        // var requestCalculatedDataClient = new RequestCalculatedDataClientV1(serviceBusSenderFactoryMock.Object);
        // var input = new RequestCalculatedDataInputV1<RequestCalculatedEnergyTimeSeriesInputV1>(
        //     Guid.NewGuid().ToString(),
        //     new RequestCalculatedEnergyTimeSeriesInputV1("B1337"));
        //
        // // Act
        // await requestCalculatedDataClient.RequestCalculatedEnergyTimeSeriesAsync(input, CancellationToken.None);

        // Assert
        await Task.CompletedTask;
    }
}
