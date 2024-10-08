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

using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.EDI.AuditLog.AuditUser;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.B2CWebApi.Security;

public class AuditUserContext(IUserContext<FrontendUser> userContext) : IAuditUserContext
{
    private readonly IUserContext<FrontendUser> _userContext = userContext;

    public AuditUser? CurrentUser
    {
        get
        {
            var currentUser = _userContext.CurrentUser;

            return new AuditUser(
                currentUser.UserId,
                currentUser.ActorId,
                ActorNumber.Create(currentUser.ActorNumber),
                ActorRoles: null,
                string.Join(", ", currentUser.Roles));
        }
    }
}
