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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

public record BundleMetadata(
    ActorNumber ReceiverNumber,
    ActorRole ReceiverRole,
    string BusinessReason,
    DocumentType DocumentType,
    MessageId? RelatedToMessageId)
{
    /// <summary>
    /// The ActorMessageQueue receiver (which ActorMessageQueue the bundle should be saved in).
    /// This is implemented to support the "hack" where a message for a MeteredDataResponsible
    /// should be added to the GridOperator queue
    /// </summary>
    public Receiver GetActorMessageQueueReceiver()
    {
        var actorMessageQueueReceiverRole = ReceiverRole;

        if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack)
        {
            actorMessageQueueReceiverRole = actorMessageQueueReceiverRole.ForActorMessageQueue();
        }

        return Receiver.Create(ReceiverNumber, actorMessageQueueReceiverRole);
    }

    public Receiver GetReceiver()
    {
        return Receiver.Create(ReceiverNumber, ReceiverRole);
    }
}
