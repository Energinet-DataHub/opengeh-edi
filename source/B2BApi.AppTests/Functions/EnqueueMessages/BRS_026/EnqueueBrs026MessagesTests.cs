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

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_026;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs026MessagesTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs026MessagesTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public async Task InitializeAsync()
    {
        _fixture.AppHostManager.ClearHostLog();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_EnqueueAcceptedBrs026Message_When_MessageIsReceived_Then_AcceptedMessagesIsEnqueued()
    {
        // Given enqueue BRS-026 service bus message
        var actorId = Guid.NewGuid().ToString();
        var energySupplierNumber = "11111111111";
        var enqueueMessagesData = new RequestCalculatedEnergyTimeSeriesInputV1(
            RequestedForActorNumber: energySupplierNumber,
            RequestedForActorRole: ActorRole.EnergySupplier.Code,
            BusinessReason: BusinessReason.BalanceFixing.Code,
            PeriodStart: "2024-01-31T23:00:00Z",
            PeriodEnd: "2024-04-31T23:00:00Z",
            EnergySupplierNumber: energySupplierNumber,
            BalanceResponsibleNumber: null,
            GridAreas: ["804"],
            MeteringPointType: null,
            SettlementMethod: null,
            SettlementVersion: null);

        var enqueueMessages = new EnqueueMessagesCommand
        {
            OrchestrationName = "Brs_026",
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            DataType = "Accepted",
            JsonData = JsonSerializer.Serialize(enqueueMessagesData),
        };

        var serviceBusMessage = new ServiceBusMessage(JsonFormatter.Default.Format(enqueueMessages))
        {
            Subject = "Enqueue_brs_026",
            ContentType = "application/json",
            MessageId = "a-message-id",
        };

        // When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Then accepted message is enqueued
        // TODO: Actually check for enqueued messages when the BRS is implemented

        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueBrs_026_Trigger)}"),
            timeLimit: TimeSpan.FromSeconds(30));
        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();
        var appThrewException = _fixture.AppHostManager.CheckIfFunctionThrewException();

        using var assertionScope = new AssertionScope();
        didFinish.Should().BeTrue($"because the {nameof(EnqueueBrs_026_Trigger)} should have been executed");
        appThrewException.Should().BeFalse();
        hostLog.Should().ContainMatch("*Received enqueue accepted message(s) for BRS 026*");
    }
}
