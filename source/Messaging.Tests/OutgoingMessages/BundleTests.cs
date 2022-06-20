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

using System;
using System.Collections.Generic;
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.OutgoingMessages;
using NodaTime;
using Xunit;

namespace Messaging.Tests.OutgoingMessages;

public class BundleTests
{
    private readonly Bundle _bundle;

    public BundleTests()
    {
        _bundle = new Bundle(SystemClock.Instance.GetCurrentInstant());
    }

    [Fact]
    public void Messages_must_originate_from_the_same_type_of_business_process()
    {
        var messages = new List<OutgoingMessage>()
        {
            CreateOutgoingMessage("ProcessType1", "SenderId"),
            CreateOutgoingMessage("ProcessType2", "SenderId"),
        };

        _bundle.Add(messages[0]);

        Assert.Throws<ProcessTypesDoesNotMatchException>(() => _bundle.Add(messages[1]));
    }

    [Fact]
    public void Messages_must_same_receiver()
    {
        var messages = new List<OutgoingMessage>()
        {
            CreateOutgoingMessage("ProcessType1", "Sender1"),
            CreateOutgoingMessage("ProcessType1", "Sender2"),
        };

        _bundle.Add(messages[0]);

        Assert.Throws<ReceiverIdsDoesNotMatchException>(() => _bundle.Add(messages[1]));
    }

    private static OutgoingMessage CreateOutgoingMessage(string processType, string receiverId)
    {
        return new OutgoingMessage(
            "DocumentType1",
            receiverId,
            "FakeId",
            "FakeId",
            processType,
            "ReceiverRole1",
            "SenderId",
            "SenderRole",
            string.Empty,
            "ReasonCode");
    }
}

#pragma warning disable
public class Bundle
{
    private readonly Instant _timestamp;
    private readonly List<OutgoingMessage> _messages = new();
    private MessageHeader? _header;

    public Bundle(Instant timestamp)
    {
        _timestamp = timestamp;
    }

    public void Add(OutgoingMessage message)
    {
        if (IsFirstMessageInBundle())
        {
            CreateHeaderFrom(message);
        }

        EnsureProcessType(message);
        EnsureReceiverId(message);

        _messages.Add(message);

    }

    private void EnsureReceiverId(OutgoingMessage message)
    {
        if (message.ReceiverId.Equals(_header.ReceiverId, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new ReceiverIdsDoesNotMatchException(message.Id.ToString());
        }
    }

    private void EnsureProcessType(OutgoingMessage message)
    {
        if (message.ProcessType.Equals(_header.ProcessType) == false)
        {
            throw new ProcessTypesDoesNotMatchException(message.Id.ToString());
        }
    }

    private bool IsFirstMessageInBundle()
    {
        return _messages.Count == 0;
    }

    private void CreateHeaderFrom(OutgoingMessage message)
    {
        _header = new MessageHeader(
            message.ProcessType,
            message.SenderId,
            message.SenderRole,
            message.ReceiverId,
            message.ReceiverRole,
            Guid.NewGuid().ToString(),
            _timestamp, message.ReasonCode);
    }
}
