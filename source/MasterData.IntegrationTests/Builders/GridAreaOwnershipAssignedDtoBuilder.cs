﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Builders;

internal sealed class GridAreaOwnershipAssignedDtoBuilder
{
    private string _gridAreaCode = "543";
    private ActorNumber _actorNumber = ActorNumber.Create("1234567890123");
    private Instant _validFrom = SystemClock.Instance.GetCurrentInstant();
    private int _sequenceNumber = 1;

    internal GridAreaOwnershipAssignedDto Build()
    {
        return new GridAreaOwnershipAssignedDto(_gridAreaCode, _validFrom, _actorNumber, _sequenceNumber);
    }

    internal GridAreaOwnershipAssignedDtoBuilder WithValidFrom(Instant newerValidFrom)
    {
        _validFrom = newerValidFrom;
        return this;
    }

    internal GridAreaOwnershipAssignedDtoBuilder WithOwnerShipActorNumber(ActorNumber actorNumber)
    {
        _actorNumber = actorNumber;
        return this;
    }

    internal GridAreaOwnershipAssignedDtoBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    internal GridAreaOwnershipAssignedDtoBuilder WithSequenceNumber(int sequenceNumber)
    {
        _sequenceNumber = sequenceNumber;
        return this;
    }
}
