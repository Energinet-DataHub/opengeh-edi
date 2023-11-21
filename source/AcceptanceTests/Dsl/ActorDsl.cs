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

using Energinet.DataHub.EDI.AcceptanceTests.Drivers;

namespace Energinet.DataHub.EDI.AcceptanceTests.Dsl;

internal sealed class ActorDsl
{
    private readonly MarketParticipantDriver _marketParticipant;
    private readonly EdiDriver _ediDriver;

    internal ActorDsl(
        MarketParticipantDriver marketParticipantDriver,
        EdiDriver ediDriver)
    {
        _marketParticipant = marketParticipantDriver;
        _ediDriver = ediDriver;
    }

    public async Task PublishResultForAsync(string actorNumber, string b2CId)
    {
        await _marketParticipant.PublishActorActivatedAsync(actorNumber, b2CId).ConfigureAwait(false);
    }

    public async Task ConfirmActorIsAvailableAsync(string actorNumber, string b2CId)
    {
        await _ediDriver.ActorExistsAsync(actorNumber, b2CId).ConfigureAwait(false);
    }
}
