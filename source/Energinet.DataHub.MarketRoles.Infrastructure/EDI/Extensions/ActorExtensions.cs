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

using System;
using System.Linq;
using Energinet.DataHub.MarketRoles.Domain.Actors;
using Actor = Energinet.DataHub.Core.App.Common.Abstractions.Actor.Actor;

namespace Energinet.DataHub.MarketRoles.Infrastructure.EDI.Extensions
{
    public static class ActorExtensions
    {
        public static Role GetRole(this Actor actor, Role role)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (role is null) throw new ArgumentNullException(nameof(role));

            var roles = actor.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (roles.Contains(role.Name))
            {
                return role;
            }

            throw new InvalidOperationException($"Actor doesn't have role {role}");
        }
    }
}
