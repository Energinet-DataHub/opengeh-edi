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
using System.Collections.Generic;
using System.Threading.Tasks;
using B2B.CimMessageAdapter.Errors;
using Energinet.DataHub.Core.App.Common.Abstractions.Actor;

namespace B2B.CimMessageAdapter.Messages
{
    public class SenderAuthorizer
    {
        private const string EnergySupplierRole = "DDQ";
        private readonly IActorContext _actorContext;
        private readonly List<ValidationError> _validationErrors = new();
        private readonly Dictionary<string, string> _rolesMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "DDQ", "electricalsupplier" },
        };

        public SenderAuthorizer(IActorContext actorContext)
        {
            _actorContext = actorContext ?? throw new ArgumentNullException(nameof(actorContext));
        }

        public Task<Result> AuthorizeAsync(string senderId, string senderRole)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (senderRole == null) throw new ArgumentNullException(nameof(senderRole));
            EnsureSenderIdMatches(senderId);
            EnsureSenderRole(senderRole);
            EnsureCurrentUserHasRequiredRole(senderRole);

            return Task.FromResult(_validationErrors.Count == 0 ? Result.Succeeded() : Result.Failure(_validationErrors.ToArray()));
        }

        private void EnsureCurrentUserHasRequiredRole(string senderRole)
        {
            _rolesMap.TryGetValue(senderRole, out var nameOfRoleClaim);
            if (nameOfRoleClaim is null)
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotHoldRequiredRoleType());
                return;
            }

            if (_actorContext.CurrentActor!.Roles.Contains(nameOfRoleClaim, StringComparison.CurrentCultureIgnoreCase) == false)
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotHoldRequiredRoleType());
            }
        }

        private void EnsureSenderRole(string senderRole)
        {
            if (senderRole.Equals(EnergySupplierRole, StringComparison.OrdinalIgnoreCase) == false)
            {
                _validationErrors.Add(new SenderRoleTypeIsNotAuthorized());
            }
        }

        private void EnsureSenderIdMatches(string senderId)
        {
            if (_actorContext.CurrentActor!.Identifier.Equals(senderId, StringComparison.OrdinalIgnoreCase) == false)
            {
                _validationErrors.Add(new SenderIdDoesNotMatchAuthenticatedUser());
            }
        }
    }
}
