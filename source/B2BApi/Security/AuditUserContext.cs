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

using Energinet.DataHub.EDI.AuditLog.AuditUser;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2BApi.Security;

public class AuditUserContext(AuthenticatedActor authenticatedActor) : IAuditUserContext
{
    public AuditUser? CurrentUser
    {
        get
        {
            if (!authenticatedActor.TryGetCurrentActorIdentity(out var currentActor))
            {
                // In some cases, we don't have an authenticated actor, e.g. when we are running in a background service like retention jobs.
                return null;
            }

            var actorRoles = currentActor?.ActorRole is not null
                ? new List<ActorRole> { currentActor.ActorRole }.AsReadOnly()
                : null;
            return new AuditUser(
                null,
                null,
                currentActor?.ActorNumber,
                actorRoles,
                currentActor?.ActorRole?.Name);
        }
    }
}
