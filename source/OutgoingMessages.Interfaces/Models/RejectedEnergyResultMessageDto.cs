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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class RejectedEnergyResultMessageDto : OutgoingMessageDto
{
    public RejectedEnergyResultMessageDto(
        ActorNumber receiverNumber,
        Guid processId,
        EventId eventId,
        string businessReason,
        ActorRole receiverRole,
        MessageId relatedToMessageId,
        RejectedEnergyResultMessageSerie series,
        ActorNumber documentReceiverNumber,
        ActorRole documentReceiverRole)
        : base(
            DocumentType.RejectRequestAggregatedMeasureData,
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            new ExternalId(Guid.NewGuid()),
            relatedToMessageId)
    {
        Series = series;
        DocumentReceiverNumber = documentReceiverNumber;
        DocumentReceiverRole = documentReceiverRole;
    }

    public RejectedEnergyResultMessageSerie Series { get; }

    public ActorNumber DocumentReceiverNumber { get; }

    public ActorRole DocumentReceiverRole { get; }
}

public record RejectedEnergyResultMessageSerie(
    TransactionId TransactionId,
    IReadOnlyCollection<RejectedEnergyResultMessageRejectReason> RejectReasons,
    TransactionId OriginalTransactionIdReference);

public record RejectedEnergyResultMessageRejectReason(string ErrorCode, string ErrorMessage);
