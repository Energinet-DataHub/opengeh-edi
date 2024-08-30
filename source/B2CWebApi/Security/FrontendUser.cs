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

namespace Energinet.DataHub.EDI.B2CWebApi.Security;

public sealed class FrontendUser
{
    public FrontendUser(Guid userId, Guid actorId, string actorNumber, string marketRole, string[] roles)
    {
        UserId = userId;
        ActorId = actorId;
        ActorNumber = actorNumber;
        MarketRole = marketRole;
        Roles = roles;
    }

    /// <summary>
    /// The user id in market participant found in the "sub" claim.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// The actor id in market participant found in the "azp" claim.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// The actor number of the corresponding actor id found in the "actornumber" claim.
    /// </summary>
    public string ActorNumber { get; }

    /// <summary>
    /// The market role ("EnergySupplier" etc.) found in the "marketroles" claim.
    /// </summary>
    public string MarketRole { get; }

    /// <summary>
    /// The user roles (permissions) found in the "roles" claim.
    /// </summary>
    public string[] Roles { get; }
}
