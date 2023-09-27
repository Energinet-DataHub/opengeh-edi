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
using Energinet.DataHub.EDI.Application.Configuration.Authentication;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.ValidationErrors;

namespace Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData
{
    public class SenderAuthorizer : ISenderAuthorizer
    {
        private readonly IMarketActorAuthenticator _marketActorAuthenticator;
        private readonly List<ValidationError> _validationErrors = new();

        public SenderAuthorizer(IMarketActorAuthenticator marketActorAuthenticator)
        {
            _marketActorAuthenticator = marketActorAuthenticator ?? throw new ArgumentNullException(nameof(marketActorAuthenticator));
        }

        public Task<Result> AuthorizeAsync(string senderId, string senderRole, string? authenticatedUser = null, string? authenticatedUserRole = null)
        {
            if (senderId == null) throw new ArgumentNullException(nameof(senderId));
            if (senderRole == null) throw new ArgumentNullException(nameof(senderRole));
            EnsureSenderIdMatches(senderId, authenticatedUser);
            EnsureSenderRole(senderRole);
            EnsureCurrentUserHasRequiredRole(senderRole, authenticatedUserRole);

            return Task.FromResult(_validationErrors.Count == 0 ? Result.Succeeded() : Result.Failure(_validationErrors.ToArray()));
        }

        private void EnsureCurrentUserHasRequiredRole(string senderRole, string? authenticatedUserRole = null)
        {
            if (!_marketActorAuthenticator.CurrentIdentity.HasRole(senderRole)
                && !(!string.IsNullOrWhiteSpace(authenticatedUserRole) && authenticatedUserRole.Equals(senderRole, StringComparison.Ordinal)))
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotHoldRequiredRoleType());
            }
        }

        private void EnsureSenderRole(string senderRole)
        {
            if (!senderRole.Equals(MarketRole.EnergySupplier.Code, StringComparison.OrdinalIgnoreCase)
                && !senderRole.Equals(MarketRole.MeteredDataResponsible.Code, StringComparison.OrdinalIgnoreCase)
                && !senderRole.Equals(MarketRole.BalanceResponsibleParty.Code, StringComparison.OrdinalIgnoreCase))
            {
                _validationErrors.Add(new SenderRoleTypeIsNotAuthorized());
            }
        }

        private void EnsureSenderIdMatches(string senderId, string? authenticatedUser = null)
        {
            if (_marketActorAuthenticator.CurrentIdentity.Number?.Value.Equals(senderId, StringComparison.OrdinalIgnoreCase) == false
                && !(!string.IsNullOrWhiteSpace(authenticatedUser) && authenticatedUser.Equals(authenticatedUser, StringComparison.Ordinal)))
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotMatchSenderId());
            }
        }
    }
}
