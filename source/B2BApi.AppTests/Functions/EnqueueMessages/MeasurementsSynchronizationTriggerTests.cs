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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IntegrationTests.Migration;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class MeasurementsSynchronizationTriggerTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public MeasurementsSynchronizationTriggerTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public async Task InitializeAsync()
    {
        _fixture.AppHostManager.ClearHostLog();
        _fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_MeteredDataTimeSeriesDH3Message_When_MessageIsReceived_Then_Brs021StartCommandIsSent()
    {
        // => Given measurements data from DH2
        var testDataResultSet = JsonPayloadConstants.SingleTimeSeriesWithSingleObservation;
        var serializer = new Serializer();

        var message = new ServiceBusMessage(testDataResultSet);

        // => When message is received
        await _fixture.MeasurementsSyncTopicResource.SenderClient.SendMessageAsync(message);

        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(MeasurementsSynchronizationTrigger));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // => Then a BRS_021_ForwardMeteredData message should be sent to the service bus
        using var assertionScope = new AssertionScope();
        var verifyServiceBusMessages = await _fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != "Brs_021_ForwardMeteredData")
                    return false;

                var parsedNotification = StartOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var expectedMeteringPointId = parsedNotification.MeteringPointId == "571051839308770693";
                var expectedDataSource = serializer.Deserialize<ForwardMeteredDataInputV1>(parsedNotification.Input).DataSource == ForwardMeteredDataInputV1.DataSourceEnum.MigrationSubsystem;

                return expectedMeteringPointId && expectedDataSource;
            })
            .VerifyCountAsync(1);
        var wasSent = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(30));
        wasSent.Should().BeTrue();
    }
}
