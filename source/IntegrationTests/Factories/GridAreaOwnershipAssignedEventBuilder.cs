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
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal sealed class GridAreaOwnershipAssignedEventBuilder
{
    private string _gridAreaCode = "543";
    private string _actorNumber = ActorNumber.Create("1234567890123").Value;
    private Timestamp _validFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-1));
    private int _sequenceNumber = 1;

    internal GridAreaOwnershipAssigned Build()
    {
        return new GridAreaOwnershipAssigned()
        {
            GridAreaCode = _gridAreaCode,
            ValidFrom = _validFrom,
            ActorNumber = _actorNumber,
            SequenceNumber = _sequenceNumber,
        };
    }

    internal GridAreaOwnershipAssignedEventBuilder WithValidFrom(Timestamp newerValidFrom)
    {
        _validFrom = newerValidFrom;
        return this;
    }

    internal GridAreaOwnershipAssignedEventBuilder WithOwnerShipActorNumber(string actorNumber)
    {
        _actorNumber = actorNumber;
        return this;
    }

    internal GridAreaOwnershipAssignedEventBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    internal GridAreaOwnershipAssignedEventBuilder WithSequenceNumber(int sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;
        return this;
    }
}
