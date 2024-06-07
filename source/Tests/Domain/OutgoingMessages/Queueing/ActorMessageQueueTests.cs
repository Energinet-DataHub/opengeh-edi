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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using NodaTime;
using Xunit;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.Tests.Domain.OutgoingMessages.Queueing;

public class ActorMessageQueueTests
{
    [Fact]
    public void Receiver_of_the_message_must_match_message_queue()
    {
        var actorMessageQueue = ActorMessageQueue.CreateFor(Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier));
        var outgoingMessage = CreateOutgoingMessage(Receiver.Create(ActorNumber.Create("1234567890124"), ActorRole.EnergySupplier), BusinessReason.BalanceFixing);

        Assert.Throws<ReceiverMismatchException>(() => actorMessageQueue.Enqueue(outgoingMessage, SystemClock.Instance.GetCurrentInstant()));
    }

    [Fact]
    public void Outgoing_message_is_assigned_to_a_bundle_when_enqueued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var outgoingMessage = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);

        actorMessageQueue.Enqueue(outgoingMessage, SystemClock.Instance.GetCurrentInstant());

        Assert.NotNull(outgoingMessage.AssignedBundleId);
    }

    [Fact]
    public void When_no_message_has_been_enqueued_peek_returns_no_bundle_id()
    {
        var actorMessageQueue = ActorMessageQueue.CreateFor(Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier));

        var result = actorMessageQueue.Peek();

        Assert.Null(result.BundleId);
    }

    [Fact]
    public void Return_bundle_id_when_messages_are_enqueued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var outgoingMessage = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        actorMessageQueue.Enqueue(outgoingMessage, SystemClock.Instance.GetCurrentInstant());

        var result = actorMessageQueue.Peek();

        Assert.Equal(outgoingMessage.AssignedBundleId, result.BundleId);
    }

    [Fact]
    public void Peek_returns_empty_bundle_if_bundle_has_been_dequeued()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var outgoingMessage = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        actorMessageQueue.Enqueue(outgoingMessage, SystemClock.Instance.GetCurrentInstant());

        var result = actorMessageQueue.Peek();

        Assert.True(actorMessageQueue.Dequeue(result.MessageId!.Value));
        Assert.Null(actorMessageQueue.Peek().BundleId);
    }

    [Fact]
    public void If_current_bundle_is_full_the_message_is_assigned_to_a_new_bundle()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        actorMessageQueue.Enqueue(CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing), SystemClock.Instance.GetCurrentInstant(), maxNumberOfMessagesInABundle: 1);
        actorMessageQueue.Enqueue(CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing), SystemClock.Instance.GetCurrentInstant(), maxNumberOfMessagesInABundle: 1);

        var firstBundle = actorMessageQueue.Peek();
        actorMessageQueue.Dequeue(firstBundle.MessageId!.Value);
        var secondBundle = actorMessageQueue.Peek();

        Assert.NotNull(firstBundle.BundleId);
        Assert.NotNull(secondBundle.BundleId);
        Assert.NotEqual(firstBundle.BundleId, secondBundle.BundleId);
    }

    [Fact]
    public void Messages_are_bundled_by_message_type_and_process_type()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        actorMessageQueue.Enqueue(CreateOutgoingMessage(receiver, BusinessReason.MoveIn, DocumentType.NotifyAggregatedMeasureData), SystemClock.Instance.GetCurrentInstant(), maxNumberOfMessagesInABundle: 2);
        actorMessageQueue.Enqueue(CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing, DocumentType.RejectRequestAggregatedMeasureData), SystemClock.Instance.GetCurrentInstant(), maxNumberOfMessagesInABundle: 2);

        var firstPeekResult = actorMessageQueue.Peek(MessageCategory.Aggregations);
        actorMessageQueue.Dequeue(firstPeekResult.MessageId!.Value);
        var secondPeekResult = actorMessageQueue.Peek(MessageCategory.Aggregations);
        actorMessageQueue.Dequeue(secondPeekResult.MessageId!.Value);

        Assert.Equal(DocumentType.NotifyAggregatedMeasureData, firstPeekResult.DocumentType);
        Assert.Equal(DocumentType.RejectRequestAggregatedMeasureData, secondPeekResult.DocumentType);
        Assert.NotEqual(firstPeekResult.BundleId, secondPeekResult.BundleId);
    }

    [Fact]
    public void Peek_returns_the_oldest_bundle()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var messageAssignedToFirstBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        var messageAssignedToSecondBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        actorMessageQueue.Enqueue(messageAssignedToFirstBundle, SystemClock.Instance.GetCurrentInstant(), 1);
        actorMessageQueue.Enqueue(messageAssignedToSecondBundle, SystemClock.Instance.GetCurrentInstant(), 1);

        var result = actorMessageQueue.Peek();

        Assert.Equal(messageAssignedToFirstBundle.AssignedBundleId, result.BundleId);
    }

    [Fact]
    public void Peek_closes_the_bundle_that_is_peeked()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);
        var messageAssignedToFirstBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        var messageAssignedToSecondBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        actorMessageQueue.Enqueue(messageAssignedToFirstBundle, SystemClock.Instance.GetCurrentInstant(), 1);

        actorMessageQueue.Peek();
        actorMessageQueue.Enqueue(messageAssignedToSecondBundle, SystemClock.Instance.GetCurrentInstant(), 1);

        Assert.NotEqual(messageAssignedToFirstBundle.AssignedBundleId, messageAssignedToSecondBundle.AssignedBundleId);
    }

    [Fact]
    public void Bundle_size_is_1_for_aggregations_message_category()
    {
        var receiver = Receiver.Create(ActorNumber.Create("1234567890123"), ActorRole.EnergySupplier);
        var actorMessageQueue = ActorMessageQueue.CreateFor(receiver);

        var messageAssignedToFirstBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing, DocumentType.NotifyAggregatedMeasureData);
        actorMessageQueue.Enqueue(messageAssignedToFirstBundle, SystemClock.Instance.GetCurrentInstant());

        var messageAssignedToSecondBundle = CreateOutgoingMessage(receiver, BusinessReason.BalanceFixing);
        actorMessageQueue.Enqueue(messageAssignedToSecondBundle, SystemClock.Instance.GetCurrentInstant());

        Assert.NotEqual(messageAssignedToFirstBundle!.AssignedBundleId, messageAssignedToSecondBundle.AssignedBundleId);
    }

    private static OutgoingMessage CreateOutgoingMessage(
        Receiver? receiver = null,
        BusinessReason? businessReason = null,
        DocumentType? messageType = null,
        ProcessType? processType = null)
    {
        receiver ??= Receiver.Create(
            ActorNumber.Create("1234567890124"),
            ActorRole.EnergySupplier);

        return new OutgoingMessage(
            EventId.From(Guid.NewGuid()),
            messageType ?? DocumentType.NotifyAggregatedMeasureData,
            receiver.Number,
            ProcessId.New().Id,
            businessReason?.Name ?? BusinessReason.BalanceFixing.Name,
            receiver.ActorRole,
            ActorNumber.Create("1234567890987"),
            ActorRole.MeteringPointAdministrator,
            string.Empty,
            Instant.FromUtc(2024, 1, 1, 0, 0),
            processType ?? ProcessType.ReceiveEnergyResults,
            null,
            null);
    }
}
