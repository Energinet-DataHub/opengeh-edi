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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions;
using NodaTime;
using Xunit;

namespace Messaging.Tests.Domain.OutgoingMessages;

public class BundleTests
{
    private readonly Bundle _bundle;

    public BundleTests()
    {
        _bundle = new Bundle(SystemClock.Instance.GetCurrentInstant());
    }

    [Fact]
    public void Can_create_message()
    {
        var outgoingMessage1 = CreateOutgoingMessage("ProcessType1");
        _bundle.Add(outgoingMessage1);
        var outgoingMessage2 = CreateOutgoingMessage("ProcessType1");
        _bundle.Add(outgoingMessage2);

        var bundledMessage = _bundle.CreateMessage();

        Assert.Equal(outgoingMessage1.MessageType, bundledMessage.MessageType);
        Assert.Equal(outgoingMessage1.ProcessType, bundledMessage.Header.ProcessType);
        Assert.Equal(outgoingMessage1.ReceiverId.Value, bundledMessage.Header.ReceiverId);
        Assert.Equal(outgoingMessage1.ReceiverRole.ToString(), bundledMessage.Header.ReceiverRole);
        Assert.Equal(outgoingMessage1.SenderId.Value, bundledMessage.Header.SenderId);
        Assert.Equal(outgoingMessage1.SenderRole.ToString(), bundledMessage.Header.SenderRole);
        Assert.Equal(2, bundledMessage.MarketActivityRecordPayloads.Count);
        Assert.NotNull(bundledMessage.Header.MessageId);
    }

    [Fact]
    public void Cannot_create_message_when_bundle_does_not_contain_any_messages()
    {
        Assert.Throws<NoMessagesInBundleException>(() => _bundle.CreateMessage());
    }

    [Fact]
    public void Messages_must_originate_from_the_same_type_of_business_process()
    {
        _bundle.Add(CreateOutgoingMessage("ProcessType1"));

        Assert.Throws<ProcessTypesDoesNotMatchException>(() => _bundle.Add(CreateOutgoingMessage("ProcessType2")));
    }

    [Fact]
    public void Messages_must_same_receiver()
    {
        _bundle.Add(CreateOutgoingMessage("ProcessType1", "1234567890123"));

        Assert.Throws<ReceiverIdsDoesNotMatchException>(() => _bundle.Add(CreateOutgoingMessage("ProcessType1", "1234567890124")));
    }

    private static OutgoingMessage CreateOutgoingMessage(string processType, string receiverId)
    {
        return new OutgoingMessage(
            MessageType.GenericNotification,
            ActorNumber.Create(receiverId),
            TransactionId.New(),
            processType,
            MarketRole.MeteringPointAdministrator,
            ActorNumber.Create("1234567890124"),
            MarketRole.EnergySupplier,
            string.Empty);
    }

    private static OutgoingMessage CreateOutgoingMessage(string processType)
    {
        return new OutgoingMessage(
            MessageType.GenericNotification,
            ActorNumber.Create("1234567890123"),
            TransactionId.New(),
            processType,
            MarketRole.MeteringPointAdministrator,
            ActorNumber.Create("1234567890124"),
            MarketRole.EnergySupplier,
            string.Empty);
    }
}
