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

using System.Globalization;
using System.Net;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions;
using Energinet.DataHub.EDI.B2BApi.Functions.BundleMessages;
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
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using BusinessReason = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.BusinessReason;
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
    public async Task Given_EnqueueAcceptedBrs021MessageWithMultipleReceivers_When_MessageIsReceived_AndWhen_MessageIsBundled_Then_AcceptedMessagesAreEnqueued_AndThen_AcceptedMessagesCanBePeeked()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagName.PM25Messages, true),
            new(FeatureFlagName.PM25CIM, true),
        ]);

        // Arrange
        // => Given enqueue BRS-021 service bus message
        const string senderActorNumber = "1234567890123";
        var senderActorRole = ActorRole.GridAccessProvider;

        const string receiver1ActorNumber = "1111111111111";
        var receiver1ActorRole = ActorRole.EnergySupplier;
        const int receiver1Quantity = 11;

        const string receiver2ActorNumber = "2222222222222";
        var receiver2ActorRole = ActorRole.EnergySupplier;
        const int receiver2Quantity = 22;

        var startDateTime = Instant.FromUtc(2025, 01, 31, 23, 00, 00);

        var receiver1Start = startDateTime;
        var receiver1End = startDateTime.Plus(Duration.FromMinutes(15));

        var receiver2Start = receiver1End;
        var receiver2End = receiver2Start.Plus(Duration.FromMinutes(15));

        var endDateTime = receiver2End;

        var enqueueMessagesData = new ForwardMeteredDataAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            MeteringPointId: "1234567890123",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            RegistrationDateTime: startDateTime.ToDateTimeOffset(),
            StartDateTime: startDateTime.ToDateTimeOffset(),
            EndDateTime: endDateTime.ToDateTimeOffset(),
            ReceiversWithMeteredData: [
                new ReceiversWithMeteredDataV1(
                    Actors: [
                        new MarketActorRecipientV1(
                            ActorNumber: ActorNumber.Create(receiver1ActorNumber).ToProcessManagerActorNumber(),
                            ActorRole: receiver1ActorRole.ToProcessManagerActorRole()),
                    ],
                    Resolution: PMValueTypes.Resolution.QuarterHourly,
                    MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
                    StartDateTime: receiver1Start.ToDateTimeOffset(),
                    EndDateTime: receiver1End.ToDateTimeOffset(),
                    MeteredData:
                    [
                        new ReceiversWithMeteredDataV1.AcceptedMeteredData(
                            Position: 1,
                            EnergyQuantity: receiver1Quantity,
                            QuantityQuality: PMValueTypes.Quality.AsProvided),
                    ]),
                new ReceiversWithMeteredDataV1(
                    Actors: [
                        new MarketActorRecipientV1(
                            ActorNumber: ActorNumber.Create(receiver2ActorNumber).ToProcessManagerActorNumber(),
                            ActorRole: receiver2ActorRole.ToProcessManagerActorRole()),
                    ],
                    Resolution: PMValueTypes.Resolution.QuarterHourly,
                    MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
                    StartDateTime: receiver2Start.ToDateTimeOffset(),
                    EndDateTime: receiver2End.ToDateTimeOffset(),
                    MeteredData:
                    [
                        new ReceiversWithMeteredDataV1.AcceptedMeteredData(
                                Position: 1,
                                EnergyQuantity: receiver2Quantity,
                                QuantityQuality: PMValueTypes.Quality.AsProvided),
                    ]),
            ]);

        var orchestrationInstanceId = Guid.NewGuid().ToString();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_021_ForwardedMeteredData.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = senderActorNumber,
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

        // Verify the function was executed
        var enqueueFunctionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_021_ForwardMeteredData));
        enqueueFunctionResult.Succeeded.Should().BeTrue("because the enqueue function should have been completed with success. Host log:\n{0}", enqueueFunctionResult.HostLog);

        // => And when message is bundled
        await _fixture.AppHostManager.TriggerFunctionAsync(nameof(OutgoingMessagesBundler));

        // Verify the bundling function was executed
        var bundleFunctionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(OutgoingMessagesBundler));
        bundleFunctionResult.Succeeded.Should().BeTrue("because the OutgoingMessagesBundler function should have been completed with success. Host log:\n{0}", bundleFunctionResult.HostLog);

        using var assertionScope = new AssertionScope();

        // => Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId && om.DocumentType == DocumentType.NotifyValidatedMeasureData)
            .ToListAsync();
        enqueuedOutgoingMessages.Should().HaveCount(2);

        // => Verify that the enqueued message can be peeked
        List<(Actor Actor, decimal EnergyQuantity, Instant Start, Instant End)> expectedReceivers =
        [
            (new Actor(ActorNumber.Create(receiver1ActorNumber), receiver1ActorRole), receiver1Quantity, receiver1Start, receiver1End),
            (new Actor(ActorNumber.Create(receiver2ActorNumber), receiver2ActorRole), receiver2Quantity, receiver2Start, receiver2End),
        ];

        foreach (var expectedReceiver in expectedReceivers)
        {
            var peekHttpRequest = await _fixture.CreatePeekHttpRequestAsync(
                actor: expectedReceiver.Actor,
                category: MessageCategory.MeasureData);

            var peekResponse = await _fixture.AppHostManager.HttpClient.SendAsync(peekHttpRequest);
            await peekResponse.EnsureSuccessStatusCodeWithLogAsync(_fixture.TestLogger);

            // Ensure status code is 200 OK, since EnsureSuccessStatusCode() also allows 204 No Content
            peekResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"because the peek request for receiver {expectedReceiver.Actor.ActorNumber} should return OK status code (with content)");

            var peekResponseContent = await peekResponse.Content.ReadAsStringAsync();
            peekResponseContent.Should().NotBeNullOrEmpty()
                .And.Contain("NotifyValidatedMeasureData", $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should be a notify validated measure data")
                .And.Contain($"\"quantity\": {expectedReceiver.EnergyQuantity}", $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected measure data")
                .And.Contain($"\"value\": \"{expectedReceiver.Start.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)}\"", $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected start")
                .And.Contain($"\"value\": \"{expectedReceiver.End.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)}\"", $"because the peeked messages for receiver {expectedReceiver.Actor.ActorNumber} should have the expected end");
        }

        // => Verify that the expected notify message was sent on the ServiceBus
        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBus(
            orchestrationInstanceId,
            ForwardMeteredDataNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs021Message_When_MessageIsReceived_AndWhen_MessageIsBundled_Then_RejectedMessageIsEnqueued_AndThen_RejectedMessageCanBePeeked()
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
            BusinessReason: BusinessReason.PeriodicFlexMetering,
            ValidationErrors: [
                new ValidationErrorDto(
                    Message: "Invalid end date",
                    ErrorCode: "999"),
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

        // Verify the function was executed
        var enqueueFunctionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_021_ForwardMeteredData));
        enqueueFunctionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", enqueueFunctionResult.HostLog);

        // => And when message is bundled
        await _fixture.AppHostManager.TriggerFunctionAsync(nameof(OutgoingMessagesBundler));

        // Verify the bundling function was executed
        var bundleFunctionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(OutgoingMessagesBundler));
        bundleFunctionResult.Succeeded.Should().BeTrue("because the OutgoingMessagesBundler function should have been completed with success. Host log:\n{0}", bundleFunctionResult.HostLog);

        // Assert
        using var assertionScope = new AssertionScope();

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
