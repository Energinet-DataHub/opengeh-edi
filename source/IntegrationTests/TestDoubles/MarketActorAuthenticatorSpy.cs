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

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;

namespace Energinet.DataHub.EDI.IntegrationTests.TestDoubles;

public class MarketActorAuthenticatorSpy : IMarketActorAuthenticator
{
    public MarketActorAuthenticatorSpy(ActorNumber senderNumber, string senderRole)
    {
        CurrentIdentity = new Authenticated("not_need", senderNumber, new List<MarketRole> { MarketRole.FromCode(senderRole) });
    }

    public MarketActorIdentity CurrentIdentity { get; }

    public Task AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
