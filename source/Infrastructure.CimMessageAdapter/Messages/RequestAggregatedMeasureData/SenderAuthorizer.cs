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
            _marketActorAuthenticator = marketActorAuthenticator;
        }

        public Task<Result> AuthorizeAsync(string senderNumber, string senderRole, string? authenticatedUser = null, string? authenticatedUserRole = null)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));
            if (senderRole == null) throw new ArgumentNullException(nameof(senderRole));
            EnsureSenderIdMatches(senderNumber, authenticatedUser);
            EnsureSenderRole(senderRole);
            EnsureCurrentUserHasRequiredRole(senderRole, authenticatedUserRole);

            return Task.FromResult(_validationErrors.Count == 0 ? Result.Succeeded() : Result.Failure(_validationErrors.ToArray()));
        }

        private bool SenderNumberIsNotEqualSenderNumberOfAuthorizedUser(string senderNumber, string? authenticatedUser)
        {
            return _marketActorAuthenticator.CurrentIdentity.Number?.Value.Equals(senderNumber, StringComparison.OrdinalIgnoreCase) == false
                && !(!string.IsNullOrWhiteSpace(authenticatedUser) && authenticatedUser.Equals(senderNumber, StringComparison.Ordinal));
        }

        private bool SenderRoleIsNotEqualRoleOfAuthorizedUser(string senderRole, string? authenticatedUserRole)
        {
            return !_marketActorAuthenticator.CurrentIdentity.HasRole(senderRole)
                && !(!string.IsNullOrWhiteSpace(authenticatedUserRole) && authenticatedUserRole.Equals(senderRole, StringComparison.Ordinal));
        }

        private void EnsureCurrentUserHasRequiredRole(string senderRole, string? authenticatedUserRole = null)
        {
            if (SenderRoleIsNotEqualRoleOfAuthorizedUser(senderRole, authenticatedUserRole))
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

        private void EnsureSenderIdMatches(string senderNumber, string? authenticatedUser = null)
        {
            if (SenderNumberIsNotEqualSenderNumberOfAuthorizedUser(senderNumber, authenticatedUser))
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotMatchSenderId());
            }
        }
    }
}
