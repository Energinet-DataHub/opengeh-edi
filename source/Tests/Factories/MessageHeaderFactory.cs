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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.Tests.Factories;

public static class MessageHeaderFactory
{
    public static MessageHeader Create(BusinessReason? businessReason = null, MarketRole? receiverRole = null, MarketRole? senderRole = null)
    {
        return new MessageHeader(
            businessReason is null ? BusinessReason.MoveIn.Name : businessReason.Name,
            "1234567890123",
            senderRole is null ? MarketRole.MeteringPointAdministrator.Name : senderRole.Name,
            "1234567890124",
            receiverRole is null ? MarketRole.EnergySupplier.Name : receiverRole.Name,
            Guid.NewGuid().ToString(),
            SystemClock.Instance.GetCurrentInstant());
    }
}
