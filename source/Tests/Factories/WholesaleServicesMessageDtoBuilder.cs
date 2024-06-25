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

public class WholesaleServicesMessageDtoBuilder
{
    private readonly ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");
    private readonly ActorNumber _chargeOwner = DataHubDetails.DataHubActorNumber;
    private readonly BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly Guid _calculationId = Guid.NewGuid();

    private ActorRole _receiverRole = ActorRole.MeteredDataResponsible;

    public WholesaleServicesMessageDto Build()
    {
        var series = new WholesaleServicesSeriesBuilder().BuildWholesaleCalculation();

        return WholesaleServicesMessageDto.Create(
            _eventId,
            _calculationId,
            _receiverNumber,
            _receiverRole,
            _chargeOwner,
            _businessReason.Name,
            series);
    }

    public WholesaleServicesMessageDtoBuilder WithReceiverRole(ActorRole receiverRole)
    {
        _receiverRole = receiverRole;
        return this;
    }
}
