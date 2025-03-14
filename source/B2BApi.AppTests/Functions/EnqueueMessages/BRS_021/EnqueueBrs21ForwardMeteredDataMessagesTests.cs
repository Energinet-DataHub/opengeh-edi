﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Net;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_021;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Components.Abstractions.BusinessValidation;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;
using PMValueTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_021;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs21ForwardMeteredDataMessagesTests : IAsyncLifetime
{
    // This string must match the subject defined in the "ProcessManagerMessageClient" from the process manager
    private const string NotifyOrchestrationInstanceSubject = "NotifyOrchestration";

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

        // Dequeue existing messages
        await using var context = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();

        var bundles = await context.Bundles.ToListAsync();
        foreach (var bundle in bundles)
        {
            bundle.TryDequeue();
        }

        await context.SaveChangesAsync();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_EnqueueAcceptedBrs021Message_When_MessageIsReceived_Then_AcceptedMessageIsEnqueued_AndThen_AcceptedMessageCanBePeeked()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagName.PM25Messages, true),
            new(FeatureFlagName.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-021 service bus message
        const string actorNumber = "1234567890123";
        var senderActorRole = ActorRole.GridAccessProvider;
        var receiverActorRole = ActorRole.EnergySupplier;

        var startDateTime = new DateTimeOffset(2025, 01, 31, 23, 00, 00, TimeSpan.Zero);
        var enqueueMessagesData = new ForwardMeteredDataAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            MeteringPointId: "1234567890123",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
            RegistrationDateTime: startDateTime,
            Resolution: PMValueTypes.Resolution.QuarterHourly,
            StartDateTime: startDateTime,
            EndDateTime: startDateTime.AddMinutes(15),
            AcceptedEnergyObservations:
            [
                new ForwardMeteredDataAcceptedV1.AcceptedEnergyObservation(
                    Position: 1,
                    EnergyQuantity: 1337,
                    QuantityQuality: PMValueTypes.Quality.Calculated),
            ],
            MarketActorRecipients:
            [
                new MarketActorRecipientV1(
                    ActorNumber: ActorNumber.Create(actorNumber).ToProcessManagerActorNumber(),
                    ActorRole: receiverActorRole.ToProcessManagerActorRole()),
            ]);

        var orchestrationInstanceId = Guid.NewGuid().ToString();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = actorNumber,
                ActorRole = senderActorRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId,
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        // Act
        var eventId = EventId.From(Guid.NewGuid());
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Assert
        using var assertionScope = new AssertionScope();

        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_021_ForwardMeteredData));
        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId && om.DocumentType == DocumentType.NotifyValidatedMeasureData)
            .ToListAsync();
        enqueuedOutgoingMessages.Should().HaveCount(1);

        // Verify that the enqueued message can be peeked
        var peekHttpRequest = await _fixture.CreatePeekHttpRequestAsync(
            actor: new Actor(ActorNumber.Create(actorNumber), receiverActorRole),
            category: MessageCategory.MeasureData);

        var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(peekHttpRequest);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

        // Ensure status code is 200 OK, since EnsureSuccessStatusCode() also allows 204 No Content
        peekResponse.StatusCode.Should().Be(HttpStatusCode.OK, "because the peek request should return OK status code (with content)");

        var peekResponseContent = await peekResponse.Content.ReadAsStringAsync();
        peekResponseContent.Should().NotBeNullOrEmpty()
            .And.Contain("NotifyValidatedMeasureData");

        // Verify that the expected notify message was sent on the ServiceBus
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            ForwardMeteredDataNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs021Message_When_MessageIsReceived_Then_RejectedMessageIsEnqueued_AndThen_RejectedMessageCanBePeeked()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagName.PM25Messages, true),
            new(FeatureFlagName.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-021 service bus message
        const string actorNumber = "1234567890123";
        var actorRole = ActorRole.GridAccessProvider;
        var enqueueMessagesData = new ForwardMeteredDataRejectedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            ForwardedByActorNumber: ActorNumber.Create(actorNumber).ToProcessManagerActorNumber(),
            ForwardedByActorRole: actorRole.ToProcessManagerActorRole(),
            ValidationErrors:
            [
                new ValidationErrorDto(
                    Message: "Invalid end date",
                    ErrorCode: "X01"),
            ]);

        var orchestrationInstanceId = Guid.NewGuid().ToString();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = actorNumber,
                ActorRole = actorRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId,
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        // Act
        var eventId = EventId.From(Guid.NewGuid());
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        // => When message is received
        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Assert
        using var assertionScope = new AssertionScope();

        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_021_ForwardMeteredData));
        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId && om.DocumentType == DocumentType.Acknowledgement)
            .ToListAsync();
        enqueuedOutgoingMessages.Should().HaveCount(1);

        // Verify that the enqueued message can be peeked
        var peekHttpRequest = await _fixture.CreatePeekHttpRequestAsync(
            actor: new Actor(ActorNumber.Create(actorNumber), actorRole),
            category: MessageCategory.MeasureData);

        var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(peekHttpRequest);
        await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

        // Ensure status code is 200 OK, since EnsureSuccessStatusCode() also allows 204 No Content
        peekResponse.StatusCode.Should().Be(HttpStatusCode.OK, "because the peek request should return OK status code (with content)");

        var peekResponseContent = await peekResponse.Content.ReadAsStringAsync();
        peekResponseContent.Should().NotBeNullOrEmpty()
            .And.Contain("Acknowledgement");

        // Verify that the expected notify message was sent on the ServiceBus
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            ForwardMeteredDataNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    private async Task<bool> ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
        string orchestrationInstanceId,
        string eventName)
    {
        var verifyServiceBusMessages = await _fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != NotifyOrchestrationInstanceSubject)
                    return false;

                var parsedNotification = NotifyOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var matchingOrchestrationId = parsedNotification.OrchestrationInstanceId == orchestrationInstanceId;
                var matchingEvent = parsedNotification.EventName == eventName;

                return matchingOrchestrationId && matchingEvent;
            })
            .VerifyCountAsync(1);

        var wasSent = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        return wasSent;
    }
}
