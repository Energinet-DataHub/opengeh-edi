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
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.AppTests.TestData.CalculationResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_026;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_026.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using ActorNumber = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ActorNumber;
using ActorRole = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ActorRole;
using BusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_026;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs026MessagesTests : IAsyncLifetime
{
    // This string must match the subject defined in the "ProcessManagerMessageClient" from the process manager
    private const string NotifyOrchestrationInstanceSubject = "NotifyOrchestration";
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
    public async Task Given_EnqueueAcceptedBrs026Message_When_MessageIsReceived_Then_AcceptedMessagesIsEnqueued()
    {
        // => Given enqueue BRS-026 service bus message
        var testDataResultSet = EnergyPerGaTestDataDescription.ResultSet1;
        var orchestrationInstanceId = Guid.NewGuid();
        var eventId = EventId.From(Guid.NewGuid());

        var (enqueueData, serviceBusMessage) = GivenEnqueueAcceptedBrs026Message(
            testDataResultSet,
            orchestrationInstanceId,
            eventId);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then accepted message is enqueued
        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_026));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();

        using var assertionScope = new AssertionScope();
        enqueuedOutgoingMessages.Should()
            .HaveCount(testDataResultSet.ExpectedMessagesCount)
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.NotifyAggregatedMeasureData);
                    om.BusinessReason.Should().Be(enqueueData.BusinessReason.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(enqueueData.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(enqueueData.RequestedByActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(enqueueData.RequestedByActorRole.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs026Message_When_MessageIsReceived_Then_RejectedMessageIsEnqueued()
    {
        // => Given enqueue rejected BRS-026 service bus message
        var eventId = EventId.From(Guid.NewGuid());
        var requestedForActorNumber = ActorNumber.Create("1111111111111");
        var requestedForActorRole = ActorRole.EnergySupplier;
        var businessReason = BusinessReason.BalanceFixing;
        var orchestrationInstanceId = Guid.NewGuid().ToString();
        var enqueueMessagesData = new RequestCalculatedEnergyTimeSeriesRejectedV1(
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
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = enqueueMessagesData.RequestedByActorNumber.Value,
                ActorRole = enqueueMessagesData.RequestedByActorRole.Name,
            },
            OrchestrationInstanceId = orchestrationInstanceId,
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then accepted message is enqueued
        var didFinish = await Awaiter.TryWaitUntilConditionAsync(
            () => _fixture.AppHostManager.CheckIfFunctionWasExecuted($"Functions.{nameof(EnqueueTrigger_Brs_026)}"),
            timeLimit: TimeSpan.FromSeconds(30));
        var hostLog = _fixture.AppHostManager.GetHostLogSnapshot();

        using (new AssertionScope())
        {
            didFinish.Should().BeTrue($"because the {nameof(EnqueueTrigger_Brs_026)} should have been executed");
            hostLog.Should().ContainMatch($"*Executed 'Functions.{nameof(EnqueueTrigger_Brs_026)}' (Succeeded,*");
            hostLog.Should().ContainMatch("*Received enqueue rejected message(s) for BRS 026*");
        }

        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var actualOutgoingMessage = await dbContext.OutgoingMessages
            .SingleOrDefaultAsync(om => om.EventId == eventId);

        actualOutgoingMessage.Should().NotBeNull();

        using var assertionScope = new AssertionScope();
        actualOutgoingMessage!.DocumentType.Should().Be(DocumentType.RejectRequestAggregatedMeasureData);
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
                var matchingEvent = parsedNotification.EventName == RequestCalculatedEnergyTimeSeriesNotifyEventsV1.EnqueueActorMessagesCompleted;

                return matchingOrchestrationId && matchingEvent;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("ActorMessagesEnqueuedV1 service bus message should be sent");
    }

    private (RequestCalculatedEnergyTimeSeriesAcceptedV1 EnqueueData, ServiceBusMessage ServiceBusMessage)
        GivenEnqueueAcceptedBrs026Message(
            EnergyPerGaTestDataDescription.ResultSet testDataResultSet,
            Guid orchestrationInstanceId,
            EventId eventId,
            string? overrideGridArea = null)
    {
        var requestedForActorNumber = testDataResultSet.ActorNumber;
        var requestedForActorRole = ActorRole.MeteredDataResponsible;
        var enqueueMessagesData = new RequestCalculatedEnergyTimeSeriesAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            BusinessReason: testDataResultSet.BusinessReason,
            RequestedForActorNumber: requestedForActorNumber,
            RequestedForActorRole: requestedForActorRole,
            RequestedByActorNumber: requestedForActorNumber,
            RequestedByActorRole: requestedForActorRole,
            PeriodStart: testDataResultSet.PeriodStart.ToDateTimeOffset(),
            PeriodEnd: testDataResultSet.PeriodEnd.ToDateTimeOffset(),
            GridAreas: [overrideGridArea ?? testDataResultSet.GridArea],
            EnergySupplierNumber: null,
            BalanceResponsibleNumber: null,
            MeteringPointType: testDataResultSet.MeteringPointType,
            SettlementMethod: null,
            SettlementVersion: null);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_026.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = enqueueMessagesData.RequestedByActorNumber.Value,
                ActorRole = enqueueMessagesData.RequestedByActorRole.Name,
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        return (enqueueMessagesData, serviceBusMessage);
    }

    private async Task<bool> ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
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
