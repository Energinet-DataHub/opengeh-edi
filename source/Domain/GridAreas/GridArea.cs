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

using Energinet.DataHub.EDI.Domain.Actors;
using NodaTime;

namespace Energinet.DataHub.EDI.Domain.GridAreas;

public class GridArea
{
    private readonly Guid _id;

    public GridArea(string gridAreaCode, Instant validFrom, ActorNumber ownerActorNumber)
    {
        _id = Guid.NewGuid();
        GridAreaCode = gridAreaCode;
        ValidFrom = validFrom;
        OwnerActorNumber = ownerActorNumber;
    }

#pragma warning disable
    private GridArea()
    {
    }

    public string GridAreaCode { get; }
    public Instant ValidFrom { get; }
    public ActorNumber OwnerActorNumber { get; }
}
