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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using FluentAssertions;
using NodaTime;
using Xunit;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.Queueing;

/// <summary>
/// Tests for the <see cref="ActorMessageQueue"/> class.
/// TODO: We probably need to refactor these tests when we decide on our approach for bundling / the ActorMessageQueue concept
/// </summary>
public class ActorMessageQueueTests
{
    [Fact]
    public void When_no_message_has_been_enqueued_peek_returns_no_bundle_id()
    {
        var actorMessageQueue = ActorMessageQueue.CreateFor(Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier));

        var result = actorMessageQueue.Peek();

        result.Should().BeNull();
    }

    [Fact]
    public void Return_bundle_id_when_messages_are_enqueued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle, outgoingMessage) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle);

        var result = actorMessageQueue.Peek();

        result.Should()
            .NotBeNull()
            .And.BeOfType<PeekResult>()
            .Subject.BundleId
            .Should()
            .Be(outgoingMessage.AssignedBundleId);
    }

    [Fact]
    public void Peek_returns_null_if_bundle_has_been_dequeued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle, outgoingMessage) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle);

        var peekResult = actorMessageQueue.Peek();
        peekResult.Should().NotBeNull();

        var dequeueResult = actorMessageQueue.Dequeue(peekResult!.MessageId);
        dequeueResult.Should().BeTrue("we should be able to dequeue what we just peeked");

        var newPeekResult = actorMessageQueue.Peek();
        newPeekResult.Should().BeNull();
    }

    [Fact]
    public void Peek_returns_new_bundle_when_old_bundle_is_dequeued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle1);
        AddBundleToQueue(actorMessageQueue, bundle2);

        // Peek and dequeue bundle 1
        var bundle1PeekResult = actorMessageQueue.Peek();
        bundle1PeekResult.Should().NotBeNull();
        bundle1PeekResult!.BundleId.Should().Be(bundle1.Id);
        bundle1PeekResult!.MessageId.Should().Be(bundle1.MessageId);

        var bundle1DequeueResult = actorMessageQueue.Dequeue(bundle1PeekResult.MessageId);
        bundle1DequeueResult.Should().BeTrue("we should be able to dequeue what we just peeked");

        // Peek bundle 2
        var bundle2PeekResult = actorMessageQueue.Peek();
        bundle2PeekResult.Should().NotBeNull();
        bundle2PeekResult!.BundleId.Should().Be(bundle2.Id);
        bundle2PeekResult!.MessageId.Should().Be(bundle2.MessageId);
    }

    [Fact(Skip = "Skipped because bundling is disabled")] //TODO: Refactor (to use case test?) if bundling is re-enabled
    public void If_current_bundle_is_full_the_message_is_assigned_to_a_new_bundle()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle1);
        AddBundleToQueue(actorMessageQueue, bundle2);

        var firstBundle = actorMessageQueue.Peek();
        firstBundle.Should().NotBeNull();

        var dequeueResult = actorMessageQueue.Dequeue(firstBundle!.MessageId);
        dequeueResult.Should().BeTrue("we should be able to dequeue what we just peeked");

        var secondBundle = actorMessageQueue.Peek();
        secondBundle.Should().NotBeNull();

        firstBundle.BundleId.Should().NotBe(secondBundle!.BundleId);
    }

    [Fact(Skip = "Skipped because bundling is disabled")] //TODO: Refactor (to use case test?) if bundling is re-enabled
    public void Messages_are_bundled_by_message_type_and_process_type()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.MoveIn, DocumentType.NotifyAggregatedMeasureData);
        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing, DocumentType.RejectRequestAggregatedMeasureData);
        AddBundleToQueue(actorMessageQueue, bundle1);
        AddBundleToQueue(actorMessageQueue, bundle2);

        var firstBundle = actorMessageQueue.Peek();
        firstBundle.Should().NotBeNull();

        var dequeueResult = actorMessageQueue.Dequeue(firstBundle!.MessageId);
        dequeueResult.Should().BeTrue("we should be able to dequeue what we just peeked");

        var secondBundle = actorMessageQueue.Peek();
        secondBundle.Should().NotBeNull();

        firstBundle.BundleId.Should().NotBe(secondBundle!.BundleId);
    }

    [Fact]
    public void Peek_returns_the_oldest_bundle()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);

        AddBundleToQueue(actorMessageQueue, bundle1);
        AddBundleToQueue(actorMessageQueue, bundle2);

        // Act
        var result = actorMessageQueue.Peek();

        // Assert
        result.Should().NotBeNull();
        result!.BundleId.Should().Be(message1.AssignedBundleId);
    }

    [Fact(Skip = "Skipped because bundling is disabled")] //TODO: Refactor (to use case test?) if bundling is re-enabled
    public void Peek_closes_the_bundle_that_is_peeked_and_new_messages_are_added_to_new_bundles()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle1);

        actorMessageQueue.Peek();
        AddBundleToQueue(actorMessageQueue, bundle2);

        Assert.NotEqual(message1.AssignedBundleId, message2.AssignedBundleId);
    }

    [Fact(Skip = "Skipped because bundling is disabled")] //TODO: Refactor (to use case test?) if bundling is re-enabled
    public void Bundle_size_is_1_for_aggregations_message_category()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var (bundle1, message1) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing, DocumentType.NotifyAggregatedMeasureData);
        AddBundleToQueue(actorMessageQueue, bundle1);

        var (bundle2, message2) = CreateBundleWithOutgoingMessage(actorMessageQueue.Id, receiver, BusinessReason.BalanceFixing);
        AddBundleToQueue(actorMessageQueue, bundle2);

        Assert.NotEqual(message1.AssignedBundleId, message2.AssignedBundleId);
    }

    /// <summary>
    /// Add bundle to the queue using reflection, since the "_bundles" field is private and only meant to be
    /// populated by entity framework
    /// </summary>
    private static void AddBundleToQueue(ActorMessageQueue actorMessageQueue, Bundle bundle)
    {
        // Add bundle to private field "_bundles" on ActorMessageQueue
        var bundlesField = actorMessageQueue
            .GetType()
            .GetField("_bundles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var bundles = bundlesField!.GetValue(actorMessageQueue) as List<Bundle>;
        bundles!.Add(bundle);

        bundlesField!.SetValue(actorMessageQueue, bundles);
    }

    private static (Bundle Bundle, OutgoingMessage OutgoingMessage) CreateBundleWithOutgoingMessage(
        ActorMessageQueueId actorMessageQueueId,
        Receiver? receiver = null,
        BusinessReason? businessReason = null,
        DocumentType? messageType = null,
        ProcessType? processType = null)
    {
        receiver ??= Receiver.Create(
            ActorNumber.Create("1234567890124"),
            ActorRole.EnergySupplier);

        var outgoingMessage = new OutgoingMessage(
            eventId: EventId.From(Guid.NewGuid()),
            documentType: messageType ?? DocumentType.NotifyAggregatedMeasureData,
            receiver: Receiver.Create(receiver.Number, receiver.ActorRole),
            documentReceiver: Receiver.Create(receiver.Number, receiver.ActorRole),
            processId: ProcessId.New().Id,
            businessReason: businessReason?.Name ?? BusinessReason.BalanceFixing.Name,
            //senderId: ActorNumber.Create("1234567890987"),
            //senderRole: ActorRole.MeteringPointAdministrator,
            serializedContent: string.Empty,
            createdAt: Instant.FromUtc(2024, 1, 1, 0, 0),
            messageCreatedFromProcess: processType ?? ProcessType.ReceiveEnergyResults,
            relatedToMessageId: null,
            gridAreaCode: null,
            externalId: new ExternalId(Guid.NewGuid()),
            calculationId: Guid.NewGuid());

        var bundle = new Bundle(
            actorMessageQueueId,
            BusinessReason.FromName(outgoingMessage.BusinessReason),
            outgoingMessage.DocumentType,
            1,
            SystemClock.Instance.GetCurrentInstant(),
            outgoingMessage.RelatedToMessageId);

        bundle.Add(outgoingMessage);

        return (bundle, outgoingMessage);
    }
}
