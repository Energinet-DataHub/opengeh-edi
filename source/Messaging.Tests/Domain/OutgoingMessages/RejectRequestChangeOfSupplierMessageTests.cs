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

using System.Collections.Generic;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Domain.Transactions;
using Xunit;

namespace Messaging.Tests.Domain.OutgoingMessages;

public class RejectRequestChangeOfSupplierMessageTests
{
    [Fact]
    public void Can_create_message()
    {
        var message = CreateMessage();

        Assert.NotNull(message);
    }

    [Fact]
    public void Must_contain_reasons()
    {
        var emptyListOfReasons = new List<Reason>();
        Assert.Throws<OutgoingMessageException>(() => CreateMessage(emptyListOfReasons));
    }

    private static RejectRequestChangeOfSupplierMessage CreateMessage(IReadOnlyList<Reason>? reasons = null)
    {
        var listOfReasons = reasons;

        if (listOfReasons == null)
        {
            listOfReasons = new List<Reason>() { new Reason("ErrorText", "ErrorCode"), };
        }

        return RejectRequestChangeOfSupplierMessage.Create(
            TransactionId.New(),
            ProcessType.MoveIn,
            string.Empty,
            ActorNumber.Create("1234567890123"),
            listOfReasons);
    }
}
