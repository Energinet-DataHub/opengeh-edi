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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.Tests.Factories;

public abstract class AcceptedWholesaleServicesMessageDtoBuilder
{
    private static readonly ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");
    private static readonly ActorNumber _chargeOwner = DataHubDetails.DataHubActorNumber;
    private static readonly Guid _processId = Guid.NewGuid();
    private static readonly BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private static readonly ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private static readonly EventId _eventId = EventId.From(Guid.NewGuid());

    public static AcceptedWholesaleServicesMessageDto Build()
    {
        var series = new AcceptedWholesaleServicesSeriesBuilder().BuildWholesaleCalculation();

        return AcceptedWholesaleServicesMessageDto.Create(
            _receiverNumber,
            _receiverRole,
            _receiverNumber,
            _receiverRole,
            _chargeOwner,
            _processId,
            _eventId,
            _businessReason.Name,
            series,
            MessageId.New());
    }
}
