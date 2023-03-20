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
using Domain.Actors;
using Domain.OutgoingMessages;
using NodaTime;

namespace Tests.Factories;

public static class MessageHeaderFactory
{
    public static MessageHeader Create(ProcessType? processType = null, MarketRole? senderRole = null, MarketRole? receiverRole = null)
    {
        return new MessageHeader(
            processType is null ? ProcessType.MoveIn.Name : processType.Name,
            "SenderId",
            senderRole is null ? MarketRole.MeteringPointAdministrator.Name : senderRole.Name,
            "ReceiverId",
            receiverRole is null ? MarketRole.EnergySupplier.Name : receiverRole.Name,
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant());
    }
}
