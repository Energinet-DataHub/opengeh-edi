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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using IncomingMessages.Infrastructure.ValidationErrors;

namespace IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData
{
    public class SenderAuthorizer : ISenderAuthorizer
    {
        private readonly AuthenticatedActor _actorAuthenticator;
        private readonly List<ValidationError> _validationErrors = new();

        public SenderAuthorizer(AuthenticatedActor actorAuthenticator)
        {
            _actorAuthenticator = actorAuthenticator;
        }

        public Task<Result> AuthorizeAsync(string senderNumber, string senderRoleCode, string? authenticatedUser = null, string? authenticatedUserRole = null)
        {
            if (senderNumber == null) throw new ArgumentNullException(nameof(senderNumber));
            if (senderRoleCode == null) throw new ArgumentNullException(nameof(senderRoleCode));
            EnsureSenderIdMatches(senderNumber, authenticatedUser);
            EnsureSenderRoleCode(senderRoleCode);
            EnsureCurrentUserHasRequiredRole(senderRoleCode, authenticatedUserRole);

            return Task.FromResult(_validationErrors.Count == 0 ? Result.Succeeded() : Result.Failure(_validationErrors.ToArray()));
        }

        private bool SenderNumberIsNotEqualSenderNumberOfAuthorizedUser(string senderNumber, string? authenticatedUser)
        {
            return _actorAuthenticator.CurrentActorIdentity.ActorNumber.Value.Equals(senderNumber, StringComparison.OrdinalIgnoreCase) == false
                && !(!string.IsNullOrWhiteSpace(authenticatedUser) && authenticatedUser.Equals(senderNumber, StringComparison.Ordinal));
        }

        private bool SenderRoleIsNotEqualRoleOfAuthorizedUser(string senderRole, string? authenticatedUserRole)
        {
            return !_actorAuthenticator.CurrentActorIdentity.HasRole(MarketRole.FromCode(senderRole))
                && !(!string.IsNullOrWhiteSpace(authenticatedUserRole) && authenticatedUserRole.Equals(senderRole, StringComparison.Ordinal));
        }

        private void EnsureCurrentUserHasRequiredRole(string senderRole, string? authenticatedUserRole = null)
        {
            if (SenderRoleIsNotEqualRoleOfAuthorizedUser(senderRole, authenticatedUserRole))
            {
                _validationErrors.Add(new AuthenticatedUserDoesNotHoldRequiredRoleType());
            }
        }

        private void EnsureSenderRoleCode(string senderRoleCode)
        {
            if (!senderRoleCode.Equals(MarketRole.EnergySupplier.Code, StringComparison.OrdinalIgnoreCase)
                && !senderRoleCode.Equals(MarketRole.MeteredDataResponsible.Code, StringComparison.OrdinalIgnoreCase)
                && !senderRoleCode.Equals(MarketRole.BalanceResponsibleParty.Code, StringComparison.OrdinalIgnoreCase))
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
