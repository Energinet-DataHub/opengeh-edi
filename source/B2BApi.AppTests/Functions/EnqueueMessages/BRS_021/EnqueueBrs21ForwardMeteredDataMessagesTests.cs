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
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;
using ActorRole = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects.ActorRole;
using BusinessReason = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects.BusinessReason;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_021;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs21ForwardMeteredDataMessagesTests : IAsyncLifetime
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs21ForwardMeteredDataMessagesTests(
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
    public async Task Given_EnqueueRejectedBrs021Message_When_MessageIsReceived_Then_RejectedMessagesIsEnqueued()
    {
        // => Given enqueue BRS-021 service bus message
        var actorId = Guid.NewGuid().ToString();
        var enqueueMessagesData = new MeteredDataForMeteringPointRejectedV1(
            "EventId",
            BusinessReason.WholesaleFixing,
            new MarketActorRecipient("1111111111111", ActorRole.GridAccessProvider),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AcknowledgementV1(
                null,
                null,
                null,
                null,
                null,
                null,
                [],
                [],
                [],
                [],
                []));

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = "Brs_021_Forward_Metered_Data",
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            Data = JsonSerializer.Serialize(enqueueMessagesData),
            DataType = nameof(MeteredDataForMeteringPointRejectedV1),
            DataFormat = EnqueueActorMessagesDataFormatV1.Json,
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: $"Enqueue_{enqueueActorMessages.OrchestrationName.ToLower()}",
            idempotencyKey: "a-message-id");

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then accepted message is enqueued
        // TODO: Actually check for enqueued messages and PM notification when the BRS is implemented

        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueTrigger_Brs_021_Forward_Metered_Data_V1)}"),
            timeLimit: TimeSpan.FromSeconds(30));
        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();
        var appThrewException = _fixture.AppHostManager.CheckIfFunctionThrewException();

        using var assertionScope = new AssertionScope();
        didFinish.Should().BeTrue($"the {nameof(EnqueueTrigger_Brs_021_Forward_Metered_Data_V1)} should have been executed");
        appThrewException.Should().BeFalse();
        hostLog.Should().ContainMatch("*Received enqueue rejected message(s) for BRS 021*");
    }
}
