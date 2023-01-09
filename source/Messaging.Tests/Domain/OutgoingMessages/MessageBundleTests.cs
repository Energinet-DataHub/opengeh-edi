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
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Xunit;

namespace Messaging.Tests.Domain.OutgoingMessages;

public class MessageBundleTests
{
    [Fact]
    public void All_messages_in_bundle_must_originate_from_the_same_type_of_process()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage("E65"),
            CreateEnqueuedMessage("E66"),
        };

        Assert.Throws<ProcessTypesDoesNotMatchException>(() =>
            MessageBundle.Create(ActorNumber.Create("1234567890123"), MessageCategory.Aggregations, messages));
    }

    [Fact]
    public void All_messages_in_bundle_must_have_same_receiver_number()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(receiverNumber: "1234567890098"),
        };

        Assert.Throws<ReceiverIdsDoesNotMatchException>(() =>
            MessageBundle.Create(ActorNumber.Create("1234567890123"), MessageCategory.Aggregations, messages));
    }

    [Fact]
    public void All_messages_in_bundle_must_have_same_receiver_role()
    {
        var messages = new List<EnqueuedMessage>()
        {
            CreateEnqueuedMessage(),
            CreateEnqueuedMessage(receiverRole: "invalid_role"),
        };

        Assert.Throws<ReceiverRoleDoesNotMatchException>(() =>
            MessageBundle.Create(ActorNumber.Create("1234567890123"), MessageCategory.Aggregations, messages));
    }

    private static EnqueuedMessage CreateEnqueuedMessage(string processType = "123", string receiverNumber = "1234567890123", string receiverRole = "Role1")
    {
        return new(Guid.NewGuid(), receiverNumber, receiverRole, "FakeActorNumber", "FakeRole", "FakeType",
            "FakeCategory", processType, string.Empty);
    }
}
