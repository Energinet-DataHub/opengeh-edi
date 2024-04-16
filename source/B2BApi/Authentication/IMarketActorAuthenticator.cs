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

using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2BApi.Authentication
{
    /// <summary>
    /// Service for authenticating an market actor
    /// </summary>
    public interface IMarketActorAuthenticator
    {
        /// <summary>
        /// Authenticates a market actor from a claims principal
        /// </summary>
        /// <param name="claimsPrincipal"></param>
        /// <param name="cancellationToken"></param>
        Task<bool> AuthenticateAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken);

        /// <summary>
        /// Authenticates a market actor
        /// </summary>
        /// <param name="actorNumber">Actor number, typically found from the external id in the `azp` claim</param>
        /// <param name="marketRole">User role, typically found from the `role` claim</param>
        bool Authenticate(ActorNumber? actorNumber, ActorRole? marketRole);
    }
}
