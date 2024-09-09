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

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal static class ActorActivatedEventBuilder
{
    private static readonly string _actorNumber = ActorNumber.Create("1234567890123").Value;
    private static readonly string _externalActorId = Guid.NewGuid().ToString();

    internal static ActorActivated Build()
    {
        return new ActorActivated()
        {
            ActorNumber = _actorNumber,
            ExternalActorId = _externalActorId,
        };
    }
}
