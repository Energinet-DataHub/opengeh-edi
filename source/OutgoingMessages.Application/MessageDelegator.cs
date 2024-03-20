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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DateTime;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class MessageDelegator
{
    private readonly MessageDelegationRepository _messageDelegationRepository;
    private readonly SystemDateTimeProvider _systemDateTimeProvider;

    public MessageDelegator(MessageDelegationRepository messageDelegationRepository, SystemDateTimeProvider systemDateTimeProvider)
    {
        _messageDelegationRepository = messageDelegationRepository;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public OutgoingMessage Delegate(OutgoingMessage messageToEnqueue)
    {
        var delegatedTo = GetDelegation(messageToEnqueue);

        if (delegatedTo is not null)
        {
            messageToEnqueue.ReceiverId = delegatedTo.Number;
            messageToEnqueue.ReceiverRole = delegatedTo.ActorRole;
        }

        return messageToEnqueue;
    }

    private Receiver? GetDelegation(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        DocumentType documentType)
    {
        return _messageDelegationRepository.GetDelegation(
            delegatedByActorNumber,
            delegatedByActorRole,
            gridAreaCode,
            documentType,
            _systemDateTimeProvider.Now());
    }
}
