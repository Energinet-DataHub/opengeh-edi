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

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

public sealed class Actor(ActorNumber actorNumber, ActorRole actorRole)
{
    public ActorNumber ActorNumber { get; init; } = actorNumber;

    public ActorRole ActorRole { get; init; } = actorRole;

    public static Actor From(string actorNumber, string actorRoleName)
    {
        return new Actor(
            ActorNumber.Create(actorNumber),
            ActorRole.FromName(actorRoleName));
    }

    public override string ToString()
    {
        return $"ActorNumber: {ActorNumber.Value}, ActorRole: {ActorRole.Name}";
    }
}
