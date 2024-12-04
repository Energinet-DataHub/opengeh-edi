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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation.ValidationErrors;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;

public class SenderAuthorizer(AuthenticatedActor actorAuthenticator) : ISenderAuthorizer
{
    private readonly List<ValidationError> _validationErrors = new();

    public Task<Result> AuthorizeAsync(IIncomingMessage message, bool allSeriesAreDelegated)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.SenderNumber);
        ArgumentNullException.ThrowIfNull(message.SenderRoleCode);

        EnsureSenderIdMatches(message.SenderNumber);
        EnsureSenderRoleCode(message, allSeriesAreDelegated);
        EnsureCurrentUserHasRequiredRole(message.SenderRoleCode, allSeriesAreDelegated);

        return Task.FromResult(
            _validationErrors.Count == 0 ? Result.Succeeded() : Result.Failure(_validationErrors.ToArray()));
    }

    private static bool HackThatAllowDdmToDoRequestsAsMdr(string senderRoleCode)
    {
        return WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack &&
            !senderRoleCode.Equals(ActorRole.GridAccessProvider.Code, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureCurrentUserHasRequiredRole(string senderRole, bool allSeriesAreDelegated)
    {
        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack && HackThatAllowDdmToActAsMdr(senderRole)) return;

        if (AllSeriesAreDelegatedToSender(allSeriesAreDelegated))
            return;

        if (!actorAuthenticator.CurrentActorIdentity.HasRole(ActorRole.FromCode(senderRole)))
        {
            _validationErrors.Add(new AuthenticatedUserDoesNotHoldRequiredRoleType());
        }
    }

    private void EnsureSenderRoleCode(IIncomingMessage message, bool allSeriesAreDelegated)
    {
        if (AllSeriesAreDelegatedToSender(allSeriesAreDelegated))
            return;

        if (message is RequestAggregatedMeasureDataMessage
            && !HackThatAllowDdmToDoRequestsAsMdr(message.SenderRoleCode))
        {
            return;
        }

        if (message.AllowedSenderRoles.Contains(ActorRole.FromCode(message.SenderRoleCode)))
        {
            return;
        }

        _validationErrors.Add(new SenderRoleTypeIsNotAuthorized());
    }

    private bool AllSeriesAreDelegatedToSender(bool allSeriesAreDelegated)
    {
        return allSeriesAreDelegated && actorAuthenticator.CurrentActorIdentity.HasAnyOfRoles(ActorRole.Delegated, ActorRole.GridAccessProvider);
    }

    private void EnsureSenderIdMatches(string senderNumber)
    {
        if (actorAuthenticator.CurrentActorIdentity.ActorNumber.Value.Equals(
                senderNumber,
                StringComparison.OrdinalIgnoreCase) == false)
        {
            _validationErrors.Add(new AuthenticatedUserDoesNotMatchSenderId());
        }
    }

    private bool HackThatAllowDdmToActAsMdr(string senderRole)
    {
        return WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack
               && senderRole == ActorRole.MeteredDataResponsible.Code
               && actorAuthenticator.CurrentActorIdentity.HasRole(ActorRole.GridAccessProvider);
    }
}
