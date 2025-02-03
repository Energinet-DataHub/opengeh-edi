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
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Components.BusinessValidation;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using ActorNumber = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects.ActorNumber;
using ActorRole = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects.ActorRole;
using BusinessReason = Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Components.Datahub.ValueObjects.BusinessReason;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_028;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs028MessagesTests : IAsyncLifetime
{
    // This string must match the subject defined in the "ProcessManagerMessageClient" from the process manager
    private const string NotifyOrchestrationInstanceSubject = "NotifyOrchestration";
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs028MessagesTests(
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
    public async Task Given_EnqueueAcceptedBrs028Message_When_MessageIsReceived_Then_AcceptedMessagesIsEnqueued()
    {
        // => Given enqueue BRS-028 service bus message
        var actorId = Guid.NewGuid().ToString();
        var requestedForActorNumber = ActorNumber.Create("1111111111111");
        var requestedForActorRole = ActorRole.EnergySupplier;
        var enqueueMessagesData = new RequestCalculatedWholesaleServicesAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            BusinessReason: BusinessReason.BalanceFixing,
            RequestedForActorNumber: requestedForActorNumber,
            RequestedForActorRole: requestedForActorRole,
            RequestedByActorNumber: requestedForActorNumber,
            RequestedByActorRole: requestedForActorRole,
            PeriodStart: Instant.FromUtc(2024, 01, 03, 23, 00).ToDateTimeOffset(),
            PeriodEnd: Instant.FromUtc(2024, 01, 04, 23, 00).ToDateTimeOffset(),
            GridAreas: ["804"],
            EnergySupplierNumber: requestedForActorNumber,
            ChargeOwnerNumber: null,
            SettlementVersion: null,
            ChargeTypes: []);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            OrchestrationInstanceId = Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: Guid.NewGuid().ToString());

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then accepted message is enqueued
        // TODO: Actually check for enqueued messages and PM notification when the BRS is implemented

        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueTrigger_Brs_028)}"),
            timeLimit: TimeSpan.FromSeconds(30));
        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();

        using var assertionScope = new AssertionScope();
        didFinish.Should().BeTrue($"because the {nameof(EnqueueTrigger_Brs_028)} should have been executed");
        hostLog.Should().ContainMatch($"*Executed 'Functions.{nameof(EnqueueTrigger_Brs_028)}' (Succeeded,*");
        hostLog.Should().ContainMatch("*Received enqueue accepted message(s) for BRS 028*");
    }

    [Fact]
    public async Task Given_EnqueueRejectBrs028Message_When_MessageIsReceived_Then_RejectedMessagesIsEnqueued()
    {
        // => Given enqueue rejected BRS-028 service bus message
        var eventId = EventId.From(Guid.NewGuid());
        var actorId = Guid.NewGuid().ToString();
        var requestedForActorNumber = ActorNumber.Create("1111111111111");
        var requestedForActorRole = ActorRole.EnergySupplier;
        var businessReason = BusinessReason.BalanceFixing;
        var orchestrationInstanceId = Guid.NewGuid().ToString();
        var enqueueMessagesData = new RequestCalculatedWholesaleServicesRejectedV1(
            OriginalMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            BusinessReason: businessReason,
            RequestedForActorNumber: requestedForActorNumber,
            RequestedForActorRole: requestedForActorRole,
            RequestedByActorNumber: requestedForActorNumber,
            RequestedByActorRole: requestedForActorRole,
            ValidationErrors: [
                new ValidationErrorDto(
                    ErrorCode: "T01",
                    Message: "Test error message 1"),
                new ValidationErrorDto(
                    ErrorCode: "T02",
                    Message: "Test error message 2"),
            ]);
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            OrchestrationInstanceId = orchestrationInstanceId,
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then rejected message is enqueued
        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueTrigger_Brs_028)}"),
            timeLimit: TimeSpan.FromSeconds(30));
        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();

        using (new AssertionScope())
        {
            didFinish.Should().BeTrue($"because the {nameof(EnqueueTrigger_Brs_028)} should have been executed");
            hostLog.Should().ContainMatch($"*Executed 'Functions.{nameof(EnqueueTrigger_Brs_028)}' (Succeeded,*");
            hostLog.Should().ContainMatch("*Received enqueue rejected message(s) for BRS 028*");
        }

        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var actualOutgoingMessage = await dbContext.OutgoingMessages
            .SingleOrDefaultAsync(om => om.EventId == eventId);

        actualOutgoingMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        actualOutgoingMessage!.DocumentType.Should().Be(DocumentType.RejectRequestWholesaleSettlement);
        actualOutgoingMessage.BusinessReason.Should().Be(businessReason.Name);
        actualOutgoingMessage.RelatedToMessageId.Should().NotBeNull();
        actualOutgoingMessage.RelatedToMessageId!.Value.Value.Should().Be(enqueueMessagesData.OriginalMessageId);
        actualOutgoingMessage.Receiver.Number.Value.Should().Be(requestedForActorNumber.Value);
        actualOutgoingMessage.Receiver.ActorRole.Name.Should().Be(requestedForActorRole.Name);

        // => Verify that the expected message was sent on the ServiceBus
        var verifyServiceBusMessages = await _fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != NotifyOrchestrationInstanceSubject)
                {
                    return false;
                }

                var parsedNotification = NotifyOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var matchingOrchestrationId = parsedNotification.OrchestrationInstanceId == orchestrationInstanceId;
                var matchingEvent = parsedNotification.EventName == RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted;

                return matchingOrchestrationId && matchingEvent;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("ActorMessagesEnqueuedV1 service bus message should be sent");
    }
}
