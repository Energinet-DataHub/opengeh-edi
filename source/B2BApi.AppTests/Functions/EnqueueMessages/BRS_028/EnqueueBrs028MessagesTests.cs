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
using Energinet.DataHub.EDI.B2BApi.AppTests.TestData.CalculationResults;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_028;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using ActorNumber = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ActorNumber;
using ActorRole = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.ActorRole;
using BusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;

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
    public async Task Given_EnqueueAcceptedBrs028Message_When_MessageIsReceived_Then_AcceptedMessagesAreEnqueued()
    {
        // => Given enqueue BRS-028 service bus message
        var testDataResultSet = AmountsPerChargeTestDataDescription.ResultSet1;
        var orchestrationInstanceId = Guid.NewGuid();
        var eventId = EventId.From(Guid.NewGuid());

        var (enqueueData, serviceBusMessage) = GivenEnqueueAcceptedBrs028Message(
            testDataResultSet,
            orchestrationInstanceId,
            eventId);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then accepted messages are enqueued
        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_028));

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
                    om.DocumentType.Should().Be(DocumentType.NotifyWholesaleServices);
                    om.BusinessReason.Should().Be(enqueueData.BusinessReason.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(enqueueData.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(enqueueData.RequestedByActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(enqueueData.RequestedByActorRole.Name);
                });

        // Verify that the expected notify message was sent on the ServiceBus
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueAcceptedBrs028Message_AndGiven_GridAreaHasNoData_When_MessageIsReceived_Then_RejectedMessageIsEnqueued()
    {
        // => Given enqueue BRS-028 service bus message
        var testDataResultSet = AmountsPerChargeTestDataDescription.ResultSet1;
        var orchestrationInstanceId = Guid.NewGuid();
        var eventId = EventId.From(Guid.NewGuid());

        var (enqueueData, serviceBusMessage) = GivenEnqueueAcceptedBrs028Message(
            testDataResultSet,
            orchestrationInstanceId,
            eventId,
            overrideGridArea: "000"); // Result set has no data for grid area 000, so an "invalid grid area" rejected message should be enqueued

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then rejected message is enqueued
        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_028));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that an outgoing message was enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();

        using var assertionScope = new AssertionScope();
        enqueuedOutgoingMessages.Should()
            .ContainSingle() // Only 1 rejected outgoing message should be enqueued.
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.RejectRequestWholesaleSettlement);
                    om.BusinessReason.Should().Be(enqueueData.BusinessReason.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(enqueueData.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(enqueueData.RequestedByActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(enqueueData.RequestedByActorRole.Name);
                });

        // Verify that the expected notify message was sent on the ServiceBus
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectBrs028Message_When_MessageIsReceived_Then_RejectedMessageIsEnqueued()
    {
        // => Given enqueue rejected BRS-028 service bus message
        var eventId = EventId.From(Guid.NewGuid());
        var actorId = Guid.NewGuid().ToString();
        var requestedForActorNumber = ActorNumber.Create("1111111111111");
        var requestedForActorRole = ActorRole.EnergySupplier;
        var businessReason = BusinessReason.BalanceFixing;
        var orchestrationInstanceId = Guid.NewGuid();
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
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // => Then rejected message is enqueued
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_028));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

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
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            RequestCalculatedWholesaleServicesNotifyEventsV1.EnqueueActorMessagesCompleted);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    private (RequestCalculatedWholesaleServicesAcceptedV1 EnqueueData, ServiceBusMessage ServiceBusMessage)
        GivenEnqueueAcceptedBrs028Message(
            AmountsPerChargeTestDataDescription.ResultSet testDataResultSet,
            Guid orchestrationInstanceId,
            EventId eventId,
            string? overrideGridArea = null)
    {
        var requestedForActorNumber = testDataResultSet.EnergySupplierNumber;
        var requestedForActorRole = ActorRole.EnergySupplier;
        var enqueueMessagesData = new RequestCalculatedWholesaleServicesAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            BusinessReason: AmountsPerChargeTestDataDescription.ResultSet1.BusinessReason,
            Resolution: null, // Request is amount per charge
            RequestedForActorNumber: requestedForActorNumber,
            RequestedForActorRole: requestedForActorRole,
            RequestedByActorNumber: requestedForActorNumber,
            RequestedByActorRole: requestedForActorRole,
            PeriodStart: testDataResultSet.PeriodStart.ToDateTimeOffset(),
            PeriodEnd: testDataResultSet.PeriodEnd.ToDateTimeOffset(),
            GridAreas: [overrideGridArea ?? testDataResultSet.GridArea],
            EnergySupplierNumber: requestedForActorNumber,
            ChargeOwnerNumber: null,
            SettlementVersion: null,
            ChargeTypes: []);

        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_028.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = Guid.NewGuid().ToString(),
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
