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

using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Authentication.MarketActors;

// TODO: can be removed when message receiver no longer needs an IMarketActorAuthenticator.
public class MarketActorAuthenticatorStub : MarketActorAuthenticator
{
    public MarketActorAuthenticatorStub(IActorRepository actorRepository)
        : base(actorRepository)
    {
    }

    public new MarketActorIdentity CurrentIdentity { get; private set; } = new Authenticated("0000000000000", ActorNumber.Create("0000000000000"), new[] { MarketRole.EnergySupplier });
}
